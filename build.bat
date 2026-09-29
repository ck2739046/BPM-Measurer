@echo off
dotnet build -c Release
if errorlevel 1 (
    echo Build failed.
    pause
    exit /b 1
)

REM Clean obj folder
echo Cleaning obj folder...
rmdir /s /q "obj" 2>nul




set RELEASE_DIR=bin\Release
set TARGET_NAME=BPM-Measurer
set TARGET_DIR=%RELEASE_DIR%\%TARGET_NAME%
set SOURCE_DIR=%RELEASE_DIR%\net8.0-windows
set FAILED=0

REM 1. Delete the old "BPM-Measurer" folder if it exists
if exist "%TARGET_DIR%" (
    echo Removing old "%TARGET_DIR%" ...
    rmdir /s /q "%TARGET_DIR%"
)

REM 2. Rename net8.0-windows to "BPM-Measurer"
if exist "%SOURCE_DIR%" (
    echo Renaming "%SOURCE_DIR%" to "%TARGET_NAME%" ...
    ren "%SOURCE_DIR%" "%TARGET_NAME%"
    if errorlevel 1 (
        echo Error: rename failed.
        set FAILED=1
    )
) else (
    echo Error: "%SOURCE_DIR%" not found.
    set FAILED=1
)

REM 3. Copy README.md and launchers into the release root (alongside the app folder)
if exist "%RELEASE_DIR%" (
    echo Copying README.md and launchers to "%RELEASE_DIR%" ...
    copy /y "README.md" "%RELEASE_DIR%\" >nul
    if errorlevel 1 (
        echo Error: copy README failed.
        set FAILED=1
    )
    copy /y "launch_english.bat" "%RELEASE_DIR%\" >nul
    if errorlevel 1 (
        echo Error: copy launch_english.bat failed.
        set FAILED=1
    )
    copy /y "launch_chinese.bat" "%RELEASE_DIR%\" >nul
    if errorlevel 1 (
        echo Error: copy launch_chinese.bat failed.
        set FAILED=1
    )
) else (
    echo Error: "%RELEASE_DIR%" not found, cannot copy README/launchers.
    set FAILED=1
)

REM 4. Copy LICENSE into the "BPM-Measurer" app folder
if exist "%TARGET_DIR%" (
    echo Copying LICENSE to "%TARGET_DIR%" ...
    copy /y "LICENSE" "%TARGET_DIR%\" >nul
    if errorlevel 1 (
        echo Error: copy LICENSE failed.
        set FAILED=1
    )
) else (
    echo Error: "%TARGET_DIR%" not found, cannot copy LICENSE.
    set FAILED=1
)

if "%FAILED%"=="1" (
    echo Packaging failed.
    pause
    exit /b 1
)

echo Done.
