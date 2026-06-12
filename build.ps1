$ErrorActionPreference = "Stop"
$env:Path = "D:\Tools\dotnet-sdk;" + $env:Path
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot ".dotnet"

Write-Host "=== Build ScreenMonitor ===" -ForegroundColor Cyan
dotnet build $PSScriptRoot/ScreenMonitor.sln 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "=== Build OK ===" -ForegroundColor Green
    Write-Host "Run: .\ScreenMonitor.UI\bin\Debug\net8.0-windows\ScreenMonitor.exe" -ForegroundColor Yellow
} else {
    Write-Host "=== Build FAILED ===" -ForegroundColor Red
    exit 1
}