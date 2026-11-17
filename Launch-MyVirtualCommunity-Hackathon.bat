@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
title MyVirtualCommunity - AI Hackathon Edition

set "LAUNCHER_SCRIPT=%~dp0scripts\launch-demo.ps1"
if not exist "%LAUNCHER_SCRIPT%" (
    echo [ERROR] Launcher script not found: "%LAUNCHER_SCRIPT%"
    pause
    exit /b 1
)

echo Launching MyVirtualCommunity - AI Hackathon Edition...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%LAUNCHER_SCRIPT%" %*
set "EXIT_CODE=%ERRORLEVEL%"

if %EXIT_CODE% NEQ 0 (
    echo.
    echo [ERROR] Application launcher exited with code %EXIT_CODE%.
    pause
)

exit /b %EXIT_CODE%
