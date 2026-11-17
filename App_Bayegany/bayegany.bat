@echo off
chcp 65001 > nul
cd /d "%~dp0"
if exist "%~dp0bin\Release\net9.0-windows\win-x64\BayeganyApp.exe" (
    start "" /d "%~dp0bin\Release\net9.0-windows\win-x64" "%~dp0bin\Release\net9.0-windows\win-x64\BayeganyApp.exe"
    exit
)
start "" /d "%~dp0bin\Release\net9.0-windows" "%~dp0bin\Release\net9.0-windows\BayeganyApp.exe"
exit
