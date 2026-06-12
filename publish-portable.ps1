$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sdkDir = "D:\Tools\dotnet-sdk"
$publishDir = Join-Path $scriptDir "publish-portable"
$appDir = Join-Path $scriptDir "ScreenMonitor.UI\bin\Release\net8.0-windows"

Write-Host "=== 构建并打包便携版 ScreenMonitor ===" -ForegroundColor Cyan

# 1. 构建
$env:PATH = "$sdkDir;$env:PATH"
$env:DOTNET_CLI_HOME = Join-Path $scriptDir ".dotnet"

Write-Host "[1/4] 构建项目..." -ForegroundColor Yellow
dotnet build $scriptDir/ScreenMonitor.sln -c Release 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "构建失败" }

# 2. 创建输出目录
Write-Host "[2/4] 创建发布目录..." -ForegroundColor Yellow
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path "$publishDir\shared\Microsoft.NETCore.App\8.0.28" -Force | Out-Null
New-Item -ItemType Directory -Path "$publishDir\shared\Microsoft.WindowsDesktop.App\8.0.28" -Force | Out-Null

# 3. 复制应用文件
Write-Host "[3/4] 复制应用文件和运行时..." -ForegroundColor Yellow
Copy-Item "$appDir\*" -Destination $publishDir -Exclude "*.pdb" -Force

# 4. 复制运行时 DLL
Write-Host "    复制 .NET 运行时 (约 160MB)..." -ForegroundColor Gray
Copy-Item "$sdkDir\shared\Microsoft.NETCore.App\8.0.28\*" -Destination "$publishDir\shared\Microsoft.NETCore.App\8.0.28" -Recurse -Force
Copy-Item "$sdkDir\shared\Microsoft.WindowsDesktop.App\8.0.28\*" -Destination "$publishDir\shared\Microsoft.WindowsDesktop.App\8.0.28" -Recurse -Force

# 5. 创建启动脚本
Write-Host "[4/4] 创建启动脚本..." -ForegroundColor Yellow
@"
@echo off
chcp 65001 >nul
set "DOTNET_ROOT=%~dp0"
"%~dp0ScreenMonitor.UI.exe"
"@ | Set-Content "$publishDir\ScreenMonitor.exe" -Encoding UTF8

Write-Host ""
Write-Host "=== 完成 ===" -ForegroundColor Green
Write-Host "便携版位于: $publishDir" -ForegroundColor Cyan
Write-Host "总大小: $([math]::Round((Get-ChildItem $publishDir -Recurse | Measure-Object Length -Sum).Sum / 1MB, 1)) MB"
Write-Host "直接运行 ScreenMonitor.exe 即可" -ForegroundColor Yellow
