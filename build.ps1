# =============================================================
# BricsLayerPlugin – Build & Package skript
# =============================================================
#
# POUŽITÍ:
#   .\build.ps1                    # Sestaví plugin + vytvoří instalátor
#   .\build.ps1 -SkipInstaller     # Pouze sestaví plugin (bez instalátoru)
#   .\build.ps1 -DownloadDotNet    # Stáhne .NET Runtime pro offline instalaci
#
# POŽADAVKY:
#   - .NET 8.0 SDK (https://dotnet.microsoft.com/download)
#   - Inno Setup 6 (https://jrsoftware.org/isdownload.php)
#     nebo: winget install JRSoftware.InnoSetup
#   - BricsCAD V25+ nainstalovaný (pro reference na DLL)
#

param(
    [switch]$SkipInstaller,
    [switch]$DownloadDotNet,
    [string]$Configuration = "Release",
    [string]$BricsCADPath = ""
)

$ErrorActionPreference = "Stop"
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
    # Zkusit najít BricsCAD automaticky
    $searchPaths = @(
        "C:\Program Files\Bricsys\BricsCAD V26",
        "C:\Program Files\Bricsys\BricsCAD V25",
        "C:\Program Files\Bricsys\BricsCAD V24",
        "${env:ProgramFiles}\Bricsys\BricsCAD V26",
        "${env:ProgramFiles}\Bricsys\BricsCAD V25",
        "${env:ProgramFiles}\Bricsys\BricsCAD V24"
    )

    foreach ($path in $searchPaths) {
        if (Test-Path "$path\BrxMgd.dll") {
            $BricsCADPath = $path
            break
        }
    }

    if (-not $BricsCADPath) {
        Write-Host "[!] BricsCAD nebyl nalezen automaticky." -ForegroundColor Yellow
        Write-Host "    Zadejte cestu rucne, napr.:" -ForegroundColor Yellow
        Write-Host '    .\build.ps1 -BricsCADPath "C:\Program Files\Bricsys\BricsCAD V25"' -ForegroundColor Gray
        Write-Host ""

        $BricsCADPath = Read-Host "Cesta k BricsCAD (nebo Enter pro preskoceni)"
        if (-not $BricsCADPath) {
            Write-Host "[!] Pokracuji bez BricsCAD referencí - build pravděpodobně selže." -ForegroundColor Red
        }
    }
}

if ($BricsCADPath) {
    Write-Host "[OK] BricsCAD nalezen: $BricsCADPath" -ForegroundColor Green
    $env:BricsCADPath = $BricsCADPath
}

# ---------------------------------------------------------------
# 2. Kontrola .NET SDK
# ---------------------------------------------------------------
Write-Host ""
Write-Host "[1/4] Kontroluji .NET SDK..." -ForegroundColor White

try {
    $dotnetVersion = & dotnet --version 2>&1
    Write-Host "       .NET SDK: $dotnetVersion" -ForegroundColor Gray
} catch {
    Write-Host "[CHYBA] .NET SDK nenalezen!" -ForegroundColor Red
    Write-Host "        Stahnete z: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# ---------------------------------------------------------------
# 3. Sestavení pluginu
# ---------------------------------------------------------------
Write-Host ""
Write-Host "[2/4] Sestavuji plugin ($Configuration)..." -ForegroundColor White

$buildArgs = @(
    "build",
    "$ScriptDir\BricsLayerPlugin.csproj",
    "-c", $Configuration,
    "--nologo"
)

if ($BricsCADPath) {
    $buildArgs += "/p:BricsCADPath=`"$BricsCADPath`""
}

& dotnet @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "[CHYBA] Sestaveni selhalo!" -ForegroundColor Red
    exit 1
}

$outputDir = "$ScriptDir\bin\$Configuration\net8.0-windows"
$dllPath = "$outputDir\BricsLayerPlugin.dll"

if (Test-Path $dllPath) {
    $fileSize = (Get-Item $dllPath).Length / 1KB
    Write-Host "       Vystup: $dllPath ($([math]::Round($fileSize, 1)) KB)" -ForegroundColor Gray
} else {
    Write-Host "[CHYBA] DLL nebyl vytvoren!" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Build uspesny" -ForegroundColor Green

# ---------------------------------------------------------------
# 4. Stažení .NET Runtime (volitelné)
# ---------------------------------------------------------------
if ($DownloadDotNet) {
    Write-Host ""
    Write-Host "[3/4] Stahuji .NET 8.0 Desktop Runtime..." -ForegroundColor White

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
    Write-Host "[3/4] Preskakuji stazeni .NET Runtime" -ForegroundColor Gray
    Write-Host "       (pouzijte -DownloadDotNet pro offline instalator)" -ForegroundColor Gray
}

# ---------------------------------------------------------------
# 5. Vytvoření instalátoru
# ---------------------------------------------------------------
if ($SkipInstaller) {
    Write-Host ""
    Write-Host "[4/4] Preskakuji tvorbu instalatoru (-SkipInstaller)" -ForegroundColor Gray
} else {
    Write-Host ""
    Write-Host "[4/4] Vytvarim instalator (Inno Setup)..." -ForegroundColor White

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
Write-Host "  Plugin DLL: $dllPath" -ForegroundColor White

if (-not $SkipInstaller -and $iscc) {
    Write-Host "  Instalator:  $distDir\BricsLayerPlugin_Setup_1.0.0.exe" -ForegroundColor White
}

Write-Host ""
Write-Host "  Dalsi kroky:" -ForegroundColor Gray
Write-Host "    1. Spustte instalator (nebo: NETLOAD v BricsCAD)" -ForegroundColor Gray
Write-Host "    2. V BricsCAD zadejte: VW_LAYERS" -ForegroundColor Gray
Write-Host ""
