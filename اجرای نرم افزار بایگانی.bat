@echo off
cd /d "%~dp0"

REM 1. Sync SQLite Database
if not exist "%~dp0App_Bayegany\bin\Release\net9.0-windows\win-x64\Data" mkdir "%~dp0App_Bayegany\bin\Release\net9.0-windows\win-x64\Data" > nul 2>&1
if exist "%~dp0App_Bayegany\Data\bayegany.db" (
    if not exist "%~dp0App_Bayegany\bin\Release\net9.0-windows\win-x64\Data\bayegany.db" (
        copy /y "%~dp0App_Bayegany\Data\bayegany.db" "%~dp0App_Bayegany\bin\Release\net9.0-windows\win-x64\Data\bayegany.db" > nul 2>&1
    )
)

REM 2. Check if Smart App Control (SAC) is Active (0x1)
reg query HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy /v VerifiedAndReputablePolicyState 2>nul | find "0x1" > nul
if %errorlevel% equ 0 goto SAC_ENABLED

REM 3. If SAC is OFF, launch native BayeganyApp directly
if exist "%~dp0App_Bayegany\bin\Debug\net9.0-windows\win-x64\BayeganyApp.exe" (
    start "" /d "%~dp0App_Bayegany\bin\Debug\net9.0-windows\win-x64" "%~dp0App_Bayegany\bin\Debug\net9.0-windows\win-x64\BayeganyApp.exe"
    exit
)
if exist "%~dp0App_Bayegany\bin\Release\net9.0-windows\win-x64\BayeganyApp.exe" (
    start "" /d "%~dp0App_Bayegany\bin\Release\net9.0-windows\win-x64" "%~dp0App_Bayegany\bin\Release\net9.0-windows\win-x64\BayeganyApp.exe"
    exit
)
if exist "%~dp0App_Bayegany\bin\Release\net9.0-windows\BayeganyApp.exe" (
    start "" /d "%~dp0App_Bayegany\bin\Release\net9.0-windows" "%~dp0App_Bayegany\bin\Release\net9.0-windows\BayeganyApp.exe"
    exit
)
exit

:SAC_ENABLED
python "%~dp0App_Bayegany\Utils\notify_sac.py"
exit
