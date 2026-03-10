@echo off
REM =============================================================
REM BricsLayerPlugin – instalační skript
REM =============================================================

setlocal

REM --- Konfigurace ---
set INSTALL_DIR=C:\BricsPlugins
set BRICSCAD_VERSION=V25
set BUILD_CONFIG=Release

echo.
echo ====================================
echo  BricsLayerPlugin - Instalace
echo ====================================
echo.

REM 1. Sestavit plugin
echo [1/4] Sestavuji plugin...
dotnet build "%~dp0..\BricsLayerPlugin.csproj" -c %BUILD_CONFIG%
if errorlevel 1 (
    echo CHYBA: Sestaveni selhalo!
    pause
    exit /b 1
)

REM 2. Vytvořit instalační složku
echo [2/4] Vytvarim instalacni slozku: %INSTALL_DIR%
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

REM 3. Kopírovat DLL
echo [3/4] Kopiruji soubory...
copy /Y "%~dp0..\bin\%BUILD_CONFIG%\net8.0-windows\BricsLayerPlugin.dll" "%INSTALL_DIR%\"
copy /Y "%~dp0..\bin\%BUILD_CONFIG%\net8.0-windows\BricsLayerPlugin.pdb" "%INSTALL_DIR%\" 2>nul

REM 4. Registrovat auto-load
echo [4/4] Registruji automaticke nacteni...
reg add "HKCU\Software\Bricsys\BricsCAD\%BRICSCAD_VERSION%\en_US\Applications\BricsLayerPlugin" /v DESCRIPTION /t REG_SZ /d "VW Layer Plugin" /f
reg add "HKCU\Software\Bricsys\BricsCAD\%BRICSCAD_VERSION%\en_US\Applications\BricsLayerPlugin" /v LOADCTRLS /t REG_DWORD /d 2 /f
reg add "HKCU\Software\Bricsys\BricsCAD\%BRICSCAD_VERSION%\en_US\Applications\BricsLayerPlugin" /v LOADER /t REG_SZ /d "%INSTALL_DIR%\BricsLayerPlugin.dll" /f
reg add "HKCU\Software\Bricsys\BricsCAD\%BRICSCAD_VERSION%\en_US\Applications\BricsLayerPlugin" /v MANAGED /t REG_DWORD /d 1 /f

echo.
echo ====================================
echo  Instalace dokoncena!
echo  Restartujte BricsCAD.
echo ====================================
echo.
pause
