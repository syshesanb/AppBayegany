@echo off
title Push AppBayegany to GitHub

set SCRIPT_DIR=%~dp0
set APP_DIR=%SCRIPT_DIR%App_Bayegany
set REMOTE_URL=https://github.com/syshesanb/AppBayegany.git

echo ============================================
echo        Git Push with Tag - AppBayegany
echo   Remote: https://github.com/syshesanb/AppBayegany
echo ============================================
echo.
echo [*] Mahale batch file: %SCRIPT_DIR%
echo.

:: 1. Barresi vojood posheh App_Bayegany
if not exist "%APP_DIR%" (
    echo ==============================================================
    echo  [!] Posheh App_Bayegany peyda nashod!
    echo.
    echo      Mahale jostoju: %APP_DIR%
    echo.
    echo  Lotfan motmaen shavid ke posheh App_Bayegany dar kenar in
    echo  batch file vojood darad va sepas mojadadan talash konid.
    echo ==============================================================
    echo.
    pause
    exit /b 1
)

echo [OK] Posheh App_Bayegany peyda shod.
echo.

cd /d "%SCRIPT_DIR%"

:: 2. Check va init kardan Git dar soorat niaz
git rev-parse --git-dir >nul 2>&1
if errorlevel 1 (
    echo [*] Git repo vojood nadarad. Dar hal initialize shodan...
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

echo [*] Current branch:
git branch --show-current
echo.

echo [*] Uncommitted changes:
git status --short
echo.

:ASK_COMMIT
set COMMIT_MSG=
set /p COMMIT_MSG=Enter commit message: 
if not defined COMMIT_MSG goto EMPTY_COMMIT
if "%COMMIT_MSG%"=="" goto EMPTY_COMMIT
goto ASK_TAG

:EMPTY_COMMIT
echo [!] Commit message cannot be empty. Try again.
echo.
goto ASK_COMMIT

:ASK_TAG
set TAG_NAME=
set /p TAG_NAME=Enter tag name (e.g. v1.0.0): 
if not defined TAG_NAME goto EMPTY_TAG
if "%TAG_NAME%"=="" goto EMPTY_TAG
goto CHECK_TAG

:EMPTY_TAG
echo [!] Tag name cannot be empty. Try again.
echo.
goto ASK_TAG

:CHECK_TAG
set FOUND_TAG=
for /f "tokens=*" %%i in ('git tag -l "%TAG_NAME%"') do set FOUND_TAG=%%i
if not defined FOUND_TAG goto ASK_DESC

echo.
echo ============================================
echo  [!] Tag vared shodeh tekrary hast: %TAG_NAME%
echo ============================================
echo.
set FORCE_TAG=
set /p FORCE_TAG=  Mikhahid in tag hazf va mojadadan sakhte shavad? (Y/N): 
echo.
if /i "%FORCE_TAG%"=="Y" (
    echo [*] Tag ghadimi dar hal hazf shodan...
    git tag -d "%TAG_NAME%"
    git push origin --delete "%TAG_NAME%" >nul 2>&1
    echo [*] Tag ghadimi hazf shod az local va remote.
    echo.
    goto ASK_DESC
)
echo [!] Lotfan yek name tag digar vared konid.
echo.
goto ASK_TAG

:ASK_DESC
set TAG_MSG=
set /p TAG_MSG=Enter tag description (optional, press Enter to skip): 
if not defined TAG_MSG set TAG_MSG=%TAG_NAME%
if "%TAG_MSG%"=="" set TAG_MSG=%TAG_NAME%

echo.
echo ============================================
echo  Summary:
echo   Commit  : %COMMIT_MSG%
echo   Tag     : %TAG_NAME%
echo   Desc    : %TAG_MSG%
echo   App Dir : %APP_DIR%
echo   Remote  : https://github.com/syshesanb/AppBayegany
echo ============================================
echo.

set CONFIRM=
set /p CONFIRM=Proceed? (Y/N): 
if /i not "%CONFIRM%"=="Y" (
    echo [!] Operation cancelled.
    pause
    exit /b 0
)

echo.
echo [0/5] Creating empty database (schema only)...
if exist "%SCRIPT_DIR%create_empty_db.py" (
    python "%SCRIPT_DIR%create_empty_db.py"
) else (
    echo [INFO] create_empty_db.py peyda nashod, rad shod.
)
echo.

echo [1/5] Adding all changes...
git add .
if errorlevel 1 goto ERROR

echo [2/5] Committing...
git commit -m "%COMMIT_MSG%"
if errorlevel 1 (
    echo [INFO] Hich taghyiri baraye commit vojood nadasht ya commit anjam shod.
)

echo [3/5] Creating tag: %TAG_NAME%...
git tag -a "%TAG_NAME%" -m "%TAG_MSG%"
if errorlevel 1 goto ERROR

echo [4/5] Pushing commits...
git push -u origin main
if errorlevel 1 goto ERROR

echo [5/5] Pushing tag...
git push origin "%TAG_NAME%"
if errorlevel 1 goto ERROR

echo.
echo ========================================================
echo  Tabrik! Amaliat Push ba movafaghiat anjam shod!
echo  Ettela'at ba tag [ %TAG_NAME% ] be dorosti Push shod.
echo  Database khali (bedune data) push shod.
echo  Makhzan: https://github.com/syshesanb/AppBayegany
echo ========================================================
echo.
pause
exit /b 0

:ERROR
echo.
echo [ERROR] Something went wrong. See message above.
echo.
pause
exit /b 1
