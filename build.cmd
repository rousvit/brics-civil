@echo off
REM =============================================================
REM BricsLayerPlugin – Jednoduché sestavení a vytvoření instalátoru
REM =============================================================
REM
REM Stačí dvakrát kliknout na tento soubor!
REM
REM Pokud se zobrazí chyba o ExecutionPolicy, spusťte z příkazové řádky:
REM   powershell -ExecutionPolicy Bypass -File build.ps1
REM

echo.
echo ============================================
echo  BricsLayerPlugin - Build
echo ============================================
echo.

REM Detekce PowerShell
where pwsh >nul 2>nul
if %errorlevel% equ 0 (
    pwsh -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
) else (
    powershell -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
)

if errorlevel 1 (
    echo.
    echo [CHYBA] Build selhal!
    echo.
    pause
    exit /b 1
)

pause
