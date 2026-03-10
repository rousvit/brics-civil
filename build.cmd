@echo off
title BricsLayerPlugin - Build
REM =============================================================
REM BricsLayerPlugin – Sestaveni a vytvoreni instalatoru
REM Staci dvakrat kliknout na tento soubor.
REM =============================================================

echo.
echo ============================================
echo  BricsLayerPlugin - Build
echo ============================================
echo.

REM Prepnout do slozky kde lezi tento .cmd soubor
echo Pracovni slozka: %~dp0
cd /d "%~dp0"
echo.

REM --- Kontroly souboru ---
echo Kontroluji soubory...

if not exist "build.ps1" (
    echo.
    echo [CHYBA] Soubor build.ps1 nebyl nalezen ve slozce:
    echo         %~dp0
    echo.
    echo Ujistete se, ze jste stahli CELY projekt.
    echo.
    goto :konec
)
echo   build.ps1              OK

if not exist "BricsLayerPlugin.csproj" (
    echo.
    echo [CHYBA] BricsLayerPlugin.csproj nebyl nalezen ve slozce:
    echo         %~dp0
    echo.
    goto :konec
)
echo   BricsLayerPlugin.csproj OK
echo.

REM --- Kontrola dotnet ---
echo Kontroluji .NET SDK...
where dotnet >nul 2>nul
if errorlevel 1 (
    echo.
    echo [CHYBA] .NET SDK neni nainstalovan!
    echo.
    echo Stahnete a nainstalujte .NET 8.0 SDK z:
    echo   https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    echo Po instalaci spustte tento soubor znovu.
    echo.
    goto :konec
)
for /f "tokens=*" %%v in ('dotnet --version 2^>nul') do echo   .NET SDK verze: %%v
echo.

REM --- Spusteni PowerShell buildu ---
echo Spoustim build skript...
echo.

REM Detekce PowerShell verze
where pwsh >nul 2>nul
if %errorlevel% equ 0 (
    echo Pouzivam: PowerShell 7+
    echo ---
    pwsh -NoProfile -ExecutionPolicy Bypass -File "build.ps1" %*
) else (
    echo Pouzivam: Windows PowerShell
    echo ---
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "build.ps1" %*
)

echo.
if errorlevel 1 (
    echo [CHYBA] Build skript skoncil s chybou.
) else (
    echo [OK] Build skript dokoncen.
)

:konec
echo.
echo ============================================
echo  Stisknete libovolnou klavesu pro zavreni.
echo ============================================
pause >nul
