@echo off
chcp 65001 > nul
title Samaneh Bayegany
cd /d "%~dp0"

:: 1. Agar file exe az ghabl sakhte shodeh bashad, an ra ejra kon
if exist "%~dp0bin\Debug\net9.0-windows\win-x64\BayeganyApp.exe" (
    start "" /d "%~dp0bin\Debug\net9.0-windows\win-x64" "%~dp0bin\Debug\net9.0-windows\win-x64\BayeganyApp.exe"
    exit
)
if exist "%~dp0bin\Debug\net9.0-windows\BayeganyApp.exe" (
    start "" /d "%~dp0bin\Debug\net9.0-windows" "%~dp0bin\Debug\net9.0-windows\BayeganyApp.exe"
    exit
)
if exist "%~dp0bin\Release\net9.0-windows\win-x64\BayeganyApp.exe" (
    start "" /d "%~dp0bin\Release\net9.0-windows\win-x64" "%~dp0bin\Release\net9.0-windows\win-x64\BayeganyApp.exe"
    exit
)
if exist "%~dp0bin\Release\net9.0-windows\BayeganyApp.exe" (
    start "" /d "%~dp0bin\Release\net9.0-windows" "%~dp0bin\Release\net9.0-windows\BayeganyApp.exe"
    exit
)

:: 2. Agar file exe peyda nashod (mesle bad az Pull kardan az GitHub), an ra build va ejra kon
if exist "%~dp0BayeganyApp.csproj" (
    echo ============================================================
    echo   Dar hal build va amadeh sazi barnameh baraye avvalin bar...
    echo ============================================================
    echo.
    dotnet run --project "%~dp0BayeganyApp.csproj"
    exit
)

echo.
echo ============================================================
echo   [ERROR] File ejraee BayeganyApp.exe peyda nashod!
echo ============================================================
echo.
pause
exit /b 1
