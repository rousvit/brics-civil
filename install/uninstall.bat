@echo off
REM =============================================================
REM BricsLayerPlugin – odinstalační skript
REM =============================================================

setlocal

set INSTALL_DIR=C:\BricsPlugins
set BRICSCAD_VERSION=V25

echo.
echo ====================================
echo  BricsLayerPlugin - Odinstalace
echo ====================================
echo.

REM 1. Odregistrovat auto-load
echo [1/2] Odstranuji registraci...
reg delete "HKCU\Software\Bricsys\BricsCAD\%BRICSCAD_VERSION%\en_US\Applications\BricsLayerPlugin" /f 2>nul

REM 2. Smazat soubory
echo [2/2] Mazu soubory...
del /Q "%INSTALL_DIR%\BricsLayerPlugin.dll" 2>nul
del /Q "%INSTALL_DIR%\BricsLayerPlugin.pdb" 2>nul

echo.
echo ====================================
echo  Odinstalace dokoncena!
echo  Restartujte BricsCAD.
echo ====================================
echo.
pause
