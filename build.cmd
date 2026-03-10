@echo off
REM =============================================================
REM BricsLayerPlugin – Jednoduché sestavení a vytvoření instalátoru
REM =============================================================
REM
REM DŮLEŽITÉ: Tento soubor musí být v kořenové složce projektu
REM           (vedle build.ps1 a BricsLayerPlugin.csproj)
REM
REM Stačí dvakrát kliknout na tento soubor!
REM

echo.
echo ============================================
echo  BricsLayerPlugin - Build
echo ============================================
echo.

REM Přepnout do složky kde leží tento .cmd soubor
cd /d "%~dp0"

REM Kontrola – je build.ps1 ve stejné složce?
if not exist "%~dp0build.ps1" (
    echo [CHYBA] Soubor build.ps1 nebyl nalezen!
    echo.
    echo Tento soubor (build.cmd) musi byt ve slozce projektu
    echo vedle souboru build.ps1 a BricsLayerPlugin.csproj.
    echo.
    echo Aktualni slozka: %~dp0
    echo.
    echo Ujistete se, ze jste stahli CELY projekt, ne jen tento soubor.
    echo.
    pause
    exit /b 1
)

REM Kontrola – je tady i .csproj?
if not exist "%~dp0BricsLayerPlugin.csproj" (
    echo [CHYBA] BricsLayerPlugin.csproj nebyl nalezen!
    echo.
    echo Tento soubor musi byt v korenove slozce projektu.
    echo Aktualni slozka: %~dp0
    echo.
    pause
    exit /b 1
)

REM Detekce PowerShell (pwsh = PowerShell 7+, powershell = Windows PowerShell 5)
where pwsh >nul 2>nul
if %errorlevel% equ 0 (
    echo Pouzivam PowerShell 7+
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
) else (
    echo Pouzivam Windows PowerShell
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
)

if errorlevel 1 (
    echo.
    echo [CHYBA] Build selhal!
    echo.
    pause
    exit /b 1
)

pause
