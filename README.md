# Screen Monitor - 屏幕监控

> 🧪 **Vibe Coding 练手项目** · 全程由 AI 辅助开发，纯探索性质

一个 Windows 桌面工具，监控开机状态下各个软件每天的使用情况，记录使用时长和时间段。纯本地运行，不联网，不改注册表，不写系统服务。

## 功能

- **自动记录**：后台轮询前台窗口，记录每个应用的使用时长
- **概览面板**：总监控时长、活跃/闲置时间、今日应用排行
- **应用详情**：点击应用可查看过去 24 小时的使用分布柱状图
- **闲置检测**：5 分钟无键盘鼠标操作自动切换至闲置记录
- **忽略列表**：在设置中添加不想被监控的进程
- **托盘驻留**：关闭窗口最小化到系统托盘，监控持续运行
- **单实例**：重复启动会自动唤出现有窗口

## 快速开始

### 前置要求
- Windows 10/11
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)（仅运行）
  或 [.NET 8.0 SDK](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)（开发）

### 运行
```bash
git clone https://github.com/<你的用户名>/screen-monitor.git
cd screen-monitor

# 构建
dotnet build ScreenMonitor.sln -c Release

# 运行
.\ScreenMonitor.UI\bin\Release\net8.0-windows\ScreenMonitor.UI.exe
```

或者直接使用 `build.bat` 构建 + `run.bat` 运行（需配置 `D:\Tools\dotnet-sdk` 路径）。

## 技术栈

- **语言**：C# 12
- **框架**：.NET 8.0 + WPF（Windows Presentation Foundation）
- **数据存储**：JSON 文件（`data.json`，零外部依赖）
- **Win32 API**：`GetForegroundWindow` / `GetLastInputInfo` 等

## 项目结构

```
ScreenMonitor.sln
├── ScreenMonitor.Core/       # 核心逻辑
│   ├── Models/               # 数据模型
│   ├── Interfaces/           # 服务接口
│   ├── Services/             # 监控、闲置检测、数据聚合
│   ├── Data/                 # JSON 数据仓库
│   └── Win32/                # Win32 P/Invoke
├── ScreenMonitor.UI/         # WPF 界面
│   └── Views/                # 概览、详情、设置
└── ScreenMonitor.Tests/      # 单元测试
```

## 截图

（待补充）


## 便携版（无需安装运行时）

将 publish-portable 文件夹整个复制到任意 Windows 10/11 电脑，运行 ScreenMonitor.cmd 即可。

> 便携版约 160MB，已内置 .NET 8.0 桌面运行时。

### 自行打包便携版

`ash
.\publish-portable.ps1
`
脚本会自动构建项目、打包运行时、生成启动脚本，输出到 publish-portable/ 目录。
## 许可

[MIT](LICENSE)

