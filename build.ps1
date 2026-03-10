# =============================================================
# BricsLayerPlugin – Build & Package skript
# =============================================================
#
# POUŽITÍ:
#   .\build.ps1                    # Sestaví plugin + vytvoří instalátor
#   .\build.ps1 -SkipInstaller     # Pouze sestaví plugin (bez instalátoru)
#   .\build.ps1 -DownloadDotNet    # Stáhne .NET Runtime pro offline instalaci
#   .\build.ps1 -ZipOnly           # Sestaví plugin + vytvoří přenosný ZIP (bez Inno Setup)
#
# POŽADAVKY:
#   - .NET 8.0 SDK (https://dotnet.microsoft.com/download)
#   - Inno Setup 6 (https://jrsoftware.org/isdownload.php)
#     nebo: winget install JRSoftware.InnoSetup
#   - BricsCAD V25+ nainstalovaný (pro reference na DLL)
#

param(
    [switch]$SkipInstaller,
    [switch]$ZipOnly,
    [switch]$DownloadDotNet,
    [string]$Configuration = "Release",
    [string]$BricsCADPath = "",
    [string]$Version = "1.0.0"
)

# DULEZITE: Nesmime pouzit "Stop" – jinak se pri chybe PowerShell ukonci
# a CMD okno se zavre driv, nez uzivatel vidi chybu.
$ErrorActionPreference = "Continue"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  BricsLayerPlugin - Build & Package" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ---------------------------------------------------------------
# 1. Detekce BricsCAD
# ---------------------------------------------------------------
if (-not $BricsCADPath) {
    Write-Host "Hledam BricsCAD na disku..." -ForegroundColor Gray

    # 1) Hledat v beznych sloskach (vcetne jazykovych variant jako "V25 en_US", "V25 cs_CZ" apod.)
    $bricsysDir = @(
        "C:\Program Files\Bricsys",
        "${env:ProgramFiles}\Bricsys"
    ) | Select-Object -Unique

    foreach ($dir in $bricsysDir) {
        if (Test-Path $dir) {
            # Najdi vsechny slozky "BricsCAD V*" serazene od nejnovejsi verze
            $candidates = Get-ChildItem -Path $dir -Directory -Filter "BricsCAD V*" -ErrorAction SilentlyContinue |
                Sort-Object Name -Descending
            foreach ($candidate in $candidates) {
                if (Test-Path (Join-Path $candidate.FullName "BrxMgd.dll")) {
                    $BricsCADPath = $candidate.FullName
                    break
                }
            }
        }
        if ($BricsCADPath) { break }
    }

    # 2) Pokud nenalezen, zkusit Windows registr
    if (-not $BricsCADPath) {
        $regPaths = @(
            "HKLM:\SOFTWARE\Bricsys\BricsCAD",
            "HKCU:\SOFTWARE\Bricsys\BricsCAD"
        )
        foreach ($regBase in $regPaths) {
            if (Test-Path $regBase) {
                $versions = Get-ChildItem $regBase -ErrorAction SilentlyContinue |
                    Sort-Object Name -Descending
                foreach ($ver in $versions) {
                    $installPath = (Get-ItemProperty "$($ver.PSPath)" -Name "InstallPath" -ErrorAction SilentlyContinue).InstallPath
                    if ($installPath -and (Test-Path "$installPath\BrxMgd.dll")) {
                        $BricsCADPath = $installPath
                        break
                    }
                }
            }
            if ($BricsCADPath) { break }
        }
    }

    if (-not $BricsCADPath) {
        Write-Host ""
        Write-Host "[!] BricsCAD nebyl nalezen automaticky." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  Zadejte UPLNOU cestu ke slozce BricsCAD," -ForegroundColor White
        Write-Host "  napr.: C:\Program Files\Bricsys\BricsCAD V25 en_US" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  (Slozka musi obsahovat soubor BrxMgd.dll)" -ForegroundColor Gray
        Write-Host ""

        $BricsCADPath = Read-Host "Cesta k BricsCAD"

        if ($BricsCADPath -and -not (Test-Path "$BricsCADPath\BrxMgd.dll")) {
            Write-Host ""
            Write-Host "[!] Soubor BrxMgd.dll nenalezen v: $BricsCADPath" -ForegroundColor Red
            Write-Host "    Build pravdepodobne selze." -ForegroundColor Yellow
            Write-Host ""
        }

        if (-not $BricsCADPath) {
            Write-Host ""
            Write-Host "[!] Bez BricsCAD build selze - zadna cesta nezadana." -ForegroundColor Red
            Write-Host ""
            Read-Host "Stisknete Enter pro zavreni"
            exit 1
        }
    }
}

Write-Host "[OK] BricsCAD nalezen: $BricsCADPath" -ForegroundColor Green
$env:BricsCADPath = $BricsCADPath

# ---------------------------------------------------------------
# 2. Kontrola .NET SDK
# ---------------------------------------------------------------
Write-Host ""
Write-Host "[1/5] Kontroluji .NET SDK..." -ForegroundColor White

$dotnetExe = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetExe) {
    Write-Host ""
    Write-Host "[CHYBA] .NET SDK neni nainstalovan!" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Stahnete a nainstalujte .NET 8.0 SDK z:" -ForegroundColor Yellow
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Po instalaci spustte build.cmd znovu." -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Stisknete Enter pro zavreni"
    exit 1
}

$dotnetVersion = & dotnet --version 2>&1
Write-Host "       .NET SDK: $dotnetVersion" -ForegroundColor Gray

# ---------------------------------------------------------------
# 3. Sestavení pluginu
# ---------------------------------------------------------------
Write-Host ""
Write-Host "[2/5] Sestavuji plugin ($Configuration)..." -ForegroundColor White

if ($BricsCADPath) {
    Write-Host "       BricsCAD: $BricsCADPath" -ForegroundColor Gray
}

$csprojPath = Join-Path $ScriptDir "BricsLayerPlugin.csproj"

# Zkopirovat BricsCAD DLL do lokalni slozky lib/ v projektu.
# Timto se obejdou vsechny problemy s cestami obsahujicimi mezery.
$libDir = Join-Path $ScriptDir "lib"
if (-not (Test-Path $libDir)) {
    New-Item -ItemType Directory -Path $libDir | Out-Null
}

$requiredDlls = @("BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll")
$allFound = $true

foreach ($dll in $requiredDlls) {
    $srcPath = Join-Path $BricsCADPath $dll
    $dstPath = Join-Path $libDir $dll

    if (Test-Path $srcPath) {
        Copy-Item -Path $srcPath -Destination $dstPath -Force
        $fileSize = [math]::Round((Get-Item $dstPath).Length / 1KB, 0)
        Write-Host "       $dll -> lib/ ($fileSize KB)" -ForegroundColor Gray
    } else {
        Write-Host "       [!] $dll NENALEZEN v: $BricsCADPath" -ForegroundColor Red
        $allFound = $false
    }
}

if (-not $allFound) {
    Write-Host ""
    Write-Host "[CHYBA] Nektere BricsCAD DLL nebyly nalezeny!" -ForegroundColor Red
    Write-Host "       Zkontrolujte instalaci BricsCAD." -ForegroundColor Yellow
    Write-Host ""
    Read-Host "Stisknete Enter pro zavreni"
    exit 1
}

# Diagnostika – zjistit, jaky .NET framework cili BrxMgd.dll
Write-Host ""
Write-Host "  Diagnostika BrxMgd.dll:" -ForegroundColor Cyan
$brxDll = Join-Path $libDir "BrxMgd.dll"
try {
    $asmName = [System.Reflection.AssemblyName]::GetAssemblyName($brxDll)
    Write-Host "       Assembly: $($asmName.FullName)" -ForegroundColor Gray
} catch {
    Write-Host "       [!] Nelze precist assembly metadata: $($_.Exception.Message)" -ForegroundColor Yellow
}
# Precist PE header – zjistit CLR runtime verzi
try {
    $fs = [System.IO.File]::OpenRead($brxDll)
    $br = New-Object System.IO.BinaryReader($fs)
    # Read PE signature offset from DOS header at 0x3C
    $fs.Position = 0x3C
    $peOffset = $br.ReadInt32()
    $fs.Position = $peOffset
    $peSignature = $br.ReadInt32()
    if ($peSignature -eq 0x4550) {
        # Read COFF header
        $machine = $br.ReadUInt16()
        $fs.Position = $peOffset + 24  # Optional header
        $magic = $br.ReadUInt16()
        if ($magic -eq 0x20B) {
            Write-Host "       PE format: PE32+ (64-bit)" -ForegroundColor Gray
        } else {
            Write-Host "       PE format: PE32 (32-bit)" -ForegroundColor Gray
        }
        # Find CLR header
        $clrHeaderOffset = if ($magic -eq 0x20B) { $peOffset + 24 + 208 + 14*8 } else { $peOffset + 24 + 192 + 14*8 }
        $fs.Position = $clrHeaderOffset
        $clrRva = $br.ReadInt32()
        $clrSize = $br.ReadInt32()
        if ($clrRva -gt 0) {
            Write-Host "       .NET assembly: ANO (CLR header nalezen)" -ForegroundColor Green
        } else {
            Write-Host "       .NET assembly: NE (nativni DLL!)" -ForegroundColor Red
            Write-Host "       Toto je nativni knihovna, nelze referencovat v .NET projektu." -ForegroundColor Red
        }
    }
    $br.Close()
    $fs.Close()
} catch {
    Write-Host "       [!] Chyba pri cteni PE header: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Sestaveni s detailnim logem pro diagnostiku referenci
Write-Host ""
$logFile = Join-Path $ScriptDir "build.log"
Write-Host "  Spoustim build s detailnim logem..." -ForegroundColor Gray
& dotnet build $csprojPath -c $Configuration --nologo -v:d *> $logFile
$buildResult = $LASTEXITCODE

if ($buildResult -ne 0) {
    Write-Host ""
    Write-Host "[CHYBA] Sestaveni selhalo! (exit code: $buildResult)" -ForegroundColor Red
    Write-Host ""

    # Zobrazit skutecne chyby kompilace z logu
    if (Test-Path $logFile) {
        $logContent = Get-Content $logFile

        # 1) Zobrazit chyby kompilace (CS* error kody)
        Write-Host "  --- Chyby kompilace ---" -ForegroundColor Cyan
        $errorLines = $logContent | Where-Object {
            $_ -match "error CS\d+" -or $_ -match ": error " -or $_ -match "error MSB\d+"
        } | Select-Object -First 30
        if ($errorLines) {
            foreach ($line in $errorLines) {
                Write-Host "  $($line.Trim())" -ForegroundColor Red
            }
        } else {
            Write-Host "  Zadne CS/MSB chyby nenalezeny." -ForegroundColor Yellow
        }

        # 2) Zobrazit varovani
        $warnLines = $logContent | Where-Object {
            $_ -match "warning CS\d+" -or $_ -match ": warning "
        } | Select-Object -First 10
        if ($warnLines) {
            Write-Host ""
            Write-Host "  --- Varovani ---" -ForegroundColor Yellow
            foreach ($line in $warnLines) {
                Write-Host "  $($line.Trim())" -ForegroundColor Yellow
            }
        }

        # 3) Pokud nebyly nalezeny zadne chyby, zobrazit konec logu
        if (-not $errorLines) {
            Write-Host ""
            Write-Host "  --- Poslednich 30 radek build logu ---" -ForegroundColor Cyan
            $logContent | Select-Object -Last 30 | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
        }

        Write-Host ""
        Write-Host "  Uplny log ulozen: $logFile" -ForegroundColor Gray
    }

    Write-Host ""
    Read-Host "Stisknete Enter pro zavreni"
    exit 1
}

$outputDir = Join-Path $ScriptDir "bin\$Configuration\net8.0-windows"
$dllPath = Join-Path $outputDir "BricsLayerPlugin.dll"

if (Test-Path $dllPath) {
    $fileSize = (Get-Item $dllPath).Length / 1KB
    Write-Host ""
    Write-Host "       Vystup: $dllPath" -ForegroundColor Gray
    Write-Host "       Velikost: $([math]::Round($fileSize, 1)) KB" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "[CHYBA] DLL nebyl vytvoren!" -ForegroundColor Red
    Write-Host "       Ocekavany soubor: $dllPath" -ForegroundColor Gray
    Read-Host "Stisknete Enter pro zavreni"
    exit 1
}

Write-Host "[OK] Build uspesny" -ForegroundColor Green

# ---------------------------------------------------------------
# 4. Stažení .NET Runtime (volitelné)
# ---------------------------------------------------------------
if ($DownloadDotNet) {
    Write-Host ""
    Write-Host "[3/5] Stahuji .NET 8.0 Desktop Runtime..." -ForegroundColor White

    $depsDir = "$ScriptDir\install\deps"
    if (-not (Test-Path $depsDir)) {
        New-Item -ItemType Directory -Path $depsDir | Out-Null
    }

    $dotnetUrl = "https://download.visualstudio.microsoft.com/download/pr/dotnet-runtime-8.0-win-x64.exe"
    $dotnetFile = "$depsDir\windowsdesktop-runtime-8.0-win-x64.exe"

    if (-not (Test-Path $dotnetFile)) {
        Write-Host "       Stahuji z: dotnet.microsoft.com..." -ForegroundColor Gray
        Write-Host ""
        Write-Host "  [POZNAMKA] Automatické stažení nemusí fungovat kvůli" -ForegroundColor Yellow
        Write-Host "  přesměrování. Pokud selže, stáhněte ručně:" -ForegroundColor Yellow
        Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Cyan
        Write-Host "  -> .NET Desktop Runtime 8.0.x -> Windows x64 Installer" -ForegroundColor Cyan
        Write-Host "  a uložte do: $depsDir" -ForegroundColor Cyan
        Write-Host ""

        try {
            Invoke-WebRequest -Uri $dotnetUrl -OutFile $dotnetFile -UseBasicParsing
            Write-Host "[OK] .NET Runtime stazen" -ForegroundColor Green
        } catch {
            Write-Host "[!] Stazeni selhalo - stahnete rucne (viz vyse)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "       Jiz stazen: $dotnetFile" -ForegroundColor Gray
    }
} else {
    Write-Host ""
    Write-Host "[3/5] Preskakuji stazeni .NET Runtime" -ForegroundColor Gray
    Write-Host "       (pouzijte -DownloadDotNet pro offline instalator)" -ForegroundColor Gray
}

# ---------------------------------------------------------------
# 5. Vytvoření přenosného ZIP balíčku
# ---------------------------------------------------------------
if ($ZipOnly -or -not $SkipInstaller) {
    Write-Host ""
    Write-Host "[+] Vytvarim prenosny ZIP balicek..." -ForegroundColor White

    $distDir = "$ScriptDir\dist"
    if (-not (Test-Path $distDir)) {
        New-Item -ItemType Directory -Path $distDir | Out-Null
    }

    $packageDir = "$distDir\BricsLayerPlugin_$Version"
    if (Test-Path $packageDir) {
        Remove-Item -Recurse -Force $packageDir
    }
    New-Item -ItemType Directory -Path $packageDir | Out-Null

    # Zkopírovat plugin DLL
    Copy-Item "$dllPath" "$packageDir\" -Force
    $pdbPath = Join-Path $outputDir "BricsLayerPlugin.pdb"
    if (Test-Path $pdbPath) {
        Copy-Item "$pdbPath" "$packageDir\" -Force
    }

    # Zkopírovat instalační skripty
    if (Test-Path "$ScriptDir\install\install.bat") {
        Copy-Item "$ScriptDir\install\install.bat" "$packageDir\" -Force
    }
    if (Test-Path "$ScriptDir\install\uninstall.bat") {
        Copy-Item "$ScriptDir\install\uninstall.bat" "$packageDir\" -Force
    }
    if (Test-Path "$ScriptDir\install\autoload.reg") {
        Copy-Item "$ScriptDir\install\autoload.reg" "$packageDir\" -Force
    }

    # Vytvořit README
    $readme = @"
BricsLayerPlugin v$Version
================================

Plugin pro BricsCAD - vrstvy a tridy ve stylu Vectorworks.

INSTALACE:
1. Spustte install.bat (jako spravce)
   - Zkopiruje plugin do C:\BricsPlugins\
   - Zaregistruje auto-load v registru
2. Spustte BricsCAD
3. Zadejte: VW_LAYERS

RUCNI NACTENI:
1. V BricsCAD zadejte: NETLOAD
2. Vyberte BricsLayerPlugin.dll
3. Zadejte: VW_LAYERS

ODINSTALACE:
   Spustte uninstall.bat

POZADAVKY:
- BricsCAD V25+ (Pro nebo Platinum)
- .NET 8.0 Desktop Runtime
  https://dotnet.microsoft.com/download/dotnet/8.0
"@
    Set-Content -Path "$packageDir\PRECTIMNE.txt" -Value $readme -Encoding UTF8

    # Vytvořit ZIP
    $zipFile = "$distDir\BricsLayerPlugin_$Version.zip"
    if (Test-Path $zipFile) { Remove-Item $zipFile -Force }
    Compress-Archive -Path "$packageDir\*" -DestinationPath $zipFile -Force

    $zipSize = [math]::Round((Get-Item $zipFile).Length / 1KB, 1)
    Write-Host "[OK] ZIP balicek vytvoren: $zipFile ($zipSize KB)" -ForegroundColor Green

    # Uklidit rozbalený adresář
    Remove-Item -Recurse -Force $packageDir
}

# ---------------------------------------------------------------
# 6. Vytvoření instalátoru (Inno Setup)
# ---------------------------------------------------------------
if ($SkipInstaller -or $ZipOnly) {
    Write-Host ""
    Write-Host "[5/5] Preskakuji tvorbu instalatoru" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "[5/5] Vytvarim instalator (Inno Setup)..." -ForegroundColor White

    # Najít Inno Setup Compiler
    $isccPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    )

    $iscc = $null
    foreach ($path in $isccPaths) {
        if (Test-Path $path) {
            $iscc = $path
            break
        }
    }

    if (-not $iscc) {
        Write-Host "[!] Inno Setup nebyl nalezen!" -ForegroundColor Yellow
        Write-Host "    Nainstalujte: winget install JRSoftware.InnoSetup" -ForegroundColor Cyan
        Write-Host "    nebo stahnete z: https://jrsoftware.org/isdownload.php" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "    Po instalaci spustte tento skript znovu." -ForegroundColor Gray
    } else {
        # Vytvořit výstupní složku
        $distDir = "$ScriptDir\dist"
        if (-not (Test-Path $distDir)) {
            New-Item -ItemType Directory -Path $distDir | Out-Null
        }

        & "$iscc" "$ScriptDir\install\BricsLayerPlugin.iss"

        if ($LASTEXITCODE -eq 0) {
            $exeFile = Get-ChildItem "$distDir\*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            Write-Host ""
            Write-Host "[OK] Instalator vytvoren!" -ForegroundColor Green
            Write-Host "       $($exeFile.FullName)" -ForegroundColor Cyan
            Write-Host "       Velikost: $([math]::Round($exeFile.Length / 1MB, 2)) MB" -ForegroundColor Gray
        } else {
            Write-Host "[CHYBA] Inno Setup selhal!" -ForegroundColor Red
        }
    }
}

# ---------------------------------------------------------------
# Souhrn
# ---------------------------------------------------------------
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Hotovo!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Plugin DLL:  $dllPath" -ForegroundColor White

$zipFile = "$ScriptDir\dist\BricsLayerPlugin_$Version.zip"
if (Test-Path $zipFile) {
    Write-Host "  ZIP balicek: $zipFile" -ForegroundColor White
}

if (-not $SkipInstaller -and -not $ZipOnly -and $iscc) {
    Write-Host "  Instalator:  $ScriptDir\dist\BricsLayerPlugin_Setup_$Version.exe" -ForegroundColor White
}

Write-Host ""
Write-Host "  Dalsi kroky:" -ForegroundColor Gray
Write-Host "    1. Spustte instalator / rozbalte ZIP (nebo: NETLOAD v BricsCAD)" -ForegroundColor Gray
Write-Host "    2. V BricsCAD zadejte: VW_LAYERS" -ForegroundColor Gray
Write-Host ""
