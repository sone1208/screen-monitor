@echo off
chcp 65001 >nul
set PATH=D:\Tools\dotnet-sdk;%PATH%
set DOTNET_CLI_HOME=%~dp0.dotnet
echo Starting ScreenMonitor...
start "" "%~dp0ScreenMonitor.UI\bin\Release\net8.0-windows\ScreenMonitor.exe"
