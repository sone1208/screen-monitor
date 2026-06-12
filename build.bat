@echo off
chcp 65001 >nul
set "PATH=D:\Tools\dotnet-sdk;%PATH%"
set "DOTNET_CLI_HOME=%~dp0.dotnet"
set "DOTNET_ROOT=D:\Tools\dotnet-sdk"
echo === Building ScreenMonitor ===
echo.
dotnet build "%~dp0ScreenMonitor.sln"
if %ERRORLEVEL% equ 0 (
    echo.
    echo === Build OK ===
    echo Run 'run.bat' to start the app
    pause
) else (
    echo.
    echo === Build FAILED ===
    pause
    exit /b 1
)
