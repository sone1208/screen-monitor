@echo off
chcp 65001 >nul
set "PATH=D:\Tools\dotnet-sdk;%PATH%"
set "DOTNET_CLI_HOME=%~dp0.dotnet"
set "DOTNET_ROOT=D:\Tools\dotnet-sdk"
echo Starting ScreenMonitor...
echo.

:: Run the app DLL directly via dotnet exec
start "" /B "D:\Tools\dotnet-sdk\dotnet.exe" exec "%~dp0ScreenMonitor.UI\bin\Debug\net8.0-windows\ScreenMonitor.UI.dll"

echo App launched in background.
echo Close this window to stop monitoring (the app will keep running).
echo To force-stop the app, run: taskkill /f /im dotnet.exe
echo.
pause
