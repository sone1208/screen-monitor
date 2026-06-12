$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sdkDir = "D:\Tools\dotnet-sdk"
$pubDir = Join-Path $scriptDir "publish-portable"
$appDir = Join-Path $scriptDir "ScreenMonitor.UI\bin\Release\net8.0-windows"

Write-Host "=== 构建便携版 ScreenMonitor ===" -ForegroundColor Cyan

$env:PATH = "$sdkDir;$env:PATH"
$env:DOTNET_CLI_HOME = Join-Path $scriptDir ".dotnet"

# 1. 构建
Write-Host "[1/4] 构建项目..." -ForegroundColor Yellow
dotnet build $scriptDir/ScreenMonitor.sln -c Release 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "构建失败" }

# 2. 创建目录
Write-Host "[2/4] 创建输出目录..." -ForegroundColor Yellow
if (Test-Path $pubDir) { Remove-Item $pubDir -Recurse -Force }
New-Item -ItemType Directory -Path "$pubDir\host\fxr\8.0.28" -Force | Out-Null
New-Item -ItemType Directory -Path "$pubDir\shared\Microsoft.NETCore.App\8.0.28" -Force | Out-Null
New-Item -ItemType Directory -Path "$pubDir\shared\Microsoft.WindowsDesktop.App\8.0.28" -Force | Out-Null

# 3. 复制应用
Write-Host "[3/4] 复制应用文件..." -ForegroundColor Yellow
Copy-Item "$appDir\*" -Destination $pubDir -Exclude "*.pdb" -Force

# 4. 复制 host + runtime
Write-Host "    复制运行时组件..." -ForegroundColor Gray
Copy-Item "$sdkDir\host\fxr\8.0.28\hostfxr.dll" "$pubDir\host\fxr\8.0.28\" -Force
Copy-Item "$sdkDir\shared\Microsoft.NETCore.App\8.0.28\*" "$pubDir\shared\Microsoft.NETCore.App\8.0.28\" -Recurse -Exclude "*.pdb","*.dbg" -Force
Copy-Item "$sdkDir\shared\Microsoft.WindowsDesktop.App\8.0.28\*" "$pubDir\shared\Microsoft.WindowsDesktop.App\8.0.28\" -Recurse -Exclude "*.pdb","*.dbg" -Force

# 5. 创建启动脚本
@"
@echo off
chcp 65001 >nul
set "DOTNET_ROOT=%~dp0"
echo Starting ScreenMonitor...
"%~dp0ScreenMonitor.UI.exe"
"@ | Set-Content "$pubDir\ScreenMonitor.cmd" -Encoding UTF8

$size = [math]::Round((Get-ChildItem $pubDir -Recurse | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host ""
Write-Host "=== 完成 ===" -ForegroundColor Green
Write-Host "便携版: $pubDir ($size MB)" -ForegroundColor Cyan
Write-Host "将整个文件夹复制到其他 Windows 电脑" -ForegroundColor Yellow
Write-Host "运行 ScreenMonitor.cmd 即可" -ForegroundColor Yellow
