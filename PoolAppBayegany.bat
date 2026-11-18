@echo off
title Pool (Pull) AppBayegany from GitHub

set SCRIPT_DIR=%~dp0
set APP_DIR=%SCRIPT_DIR%App_Bayegany
set REMOTE_URL=https://github.com/syshesanb/AppBayegany.git

echo ============================================
echo       Git Pull with Tag - AppBayegany
echo   Remote: https://github.com/syshesanb/AppBayegany
echo ============================================
echo.
echo [*] Mahale batch file: %SCRIPT_DIR%
echo.

:: 1. Barresi vojood posheh App_Bayegany va ijad an dar soorat adam vojood
if not exist "%APP_DIR%" (
    echo [!] Posheh App_Bayegany dar in masir peyda nashod.
    echo [*] Dar hal sakhtan posheh jadid ba name App_Bayegany...
    mkdir "%APP_DIR%"
    if errorlevel 1 (
        echo [ERROR] Sakhte posheh App_Bayegany ba khata movajeh shod!
        pause
        exit /b 1
    )
    echo [OK] Posheh App_Bayegany sakhte shod: %APP_DIR%
    echo.
) else (
    echo [OK] Posheh App_Bayegany peyda shod: %APP_DIR%
    echo.
)

cd /d "%SCRIPT_DIR%"

:: 2. Check va init kardan Git dar soorat niaz
git rev-parse --git-dir >nul 2>&1
if errorlevel 1 (
    echo [*] Git repo dar in masir peyda nashod. Dar hal initialize shodan...
    git init
    git branch -M main
)

:: 3. Tanzim Remote Origin
git remote get-url origin >nul 2>&1
if errorlevel 1 (
    git remote add origin %REMOTE_URL%
    echo [*] Remote added: %REMOTE_URL%
) else (
    git remote set-url origin %REMOTE_URL%
    echo [*] Remote set to: %REMOTE_URL%
)
echo.

:: 4. Daryaft tag-ha va etela'at az remote
echo [*] Fetching all tags and branches from remote...
git fetch --all --tags
if errorlevel 1 (
    echo [ERROR] Ertebat ba GitHub bargharar nashod. Internet ra check konid.
    pause
    exit /b 1
)
echo.

echo [*] Available tags on remote:
echo ----------------------------------------
git tag -l --sort=-version:refname
echo ----------------------------------------
echo.

:ASK_TAG
set TAG_NAME=
set /p TAG_NAME=Tag name ra vared konid (e.g. v1.0.0): 
if not defined TAG_NAME goto EMPTY_TAG
if "%TAG_NAME%"=="" goto EMPTY_TAG

:: Barresi vojood tag ba for loop motmaen
set CHECK_FOUND=
for /f "tokens=*" %%T in ('git tag -l "%TAG_NAME%"') do (
    if /i "%%T"=="%TAG_NAME%" set CHECK_FOUND=yes
)

if not defined CHECK_FOUND (
    echo.
    echo [!] Tag "%TAG_NAME%" peyda nashod. Lotfan tag sahih entekhab konid.
    echo.
    goto ASK_TAG
)
goto CONFIRM

:EMPTY_TAG
echo [!] Tag name nemitavanad khali bashad. Dobareh vared konid.
echo.
goto ASK_TAG

:CONFIRM
echo.
echo ============================================
echo  Summary:
echo   Tag     : %TAG_NAME%
echo   App Dir : %APP_DIR%
echo   Remote  : https://github.com/syshesanb/AppBayegany
echo   Action  : Checkout and Pull tag
echo ============================================
echo.

set CONFIRM=
set /p CONFIRM=Adameh midahid? (Y/N): 
if /i not "%CONFIRM%"=="Y" (
    echo [!] Amaliat laghv shod.
    pause
    exit /b 0
)

echo.
echo [1/2] Saving current local changes (stash)...
git stash
echo.

echo [2/2] Switching to tag: %TAG_NAME%...
git checkout "tags/%TAG_NAME%" -B "tag_%TAG_NAME%"
if errorlevel 1 goto ERROR

echo.
echo ========================================================
echo  Movafagh! Ettela'at ba tag [ %TAG_NAME% ]
echo  be dorosti az GitHub Pool (Pull) shod.
echo  Posheh App_Bayegany be roozresani shod.
echo  Hale hazer dar branch: tag_%TAG_NAME%
echo ========================================================
echo.
pause
exit /b 0

:ERROR
echo.
echo [ERROR] Moshkel dar Pool (Pull) pish amad. Khatay bala ra bebinid.
echo.
pause
exit /b 1
