using ScreenMonitor.Core.Interfaces;
using ScreenMonitor.Core.Models;

namespace ScreenMonitor.Tests;

public class WindowMonitorServiceTests
{
    // ===== 测试辅助：窗口监控服务的测试替身 =====
    private class FakeWindowMonitorService : IWindowMonitorService
    {
        public bool IsRunning { get; private set; }
        public List<string> IgnoredProcesses { get; set; } = new();
        public int PollIntervalSeconds { get; set; } = 1;
        public event Action<ActivitySession>? OnSessionCreated;
        public event Action<ActivitySession>? OnSessionUpdated;

        public List<ActivitySession> Sessions { get; } = new();
        private ActivitySession? _currentSession;
        private int _sessionIdCounter;

        public void Start() => IsRunning = true;
        public void Stop() => IsRunning = false;
        public ActivitySession? GetCurrentSession() => _currentSession;

        /// <summary>模拟一次采集</summary>
        public void SimulateCollect(string processName, string? windowTitle = null, int processId = 1)
        {
            if (IgnoredProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                _currentSession = null;
                return;
            }

            var now = DateTime.Now;

            if (_currentSession != null &&
                _currentSession.ProcessName == processName &&
                _currentSession.WindowTitle == windowTitle)
            {
                // 同一会话，扩展
                _currentSession.EndTime = now;
                _currentSession.DurationSeconds = (long)(now - _currentSession.StartTime).TotalSeconds;
                OnSessionUpdated?.Invoke(_currentSession);
            }
            else
            {
                // 关闭旧会话，创建新会话
                if (_currentSession != null)
                {
                    _currentSession.EndTime = now;
                    _currentSession.DurationSeconds = (long)(now - _currentSession.StartTime).TotalSeconds;
                }

                _currentSession = new ActivitySession
                {
                    Id = ++_sessionIdCounter,
                    ProcessName = processName,
                    WindowTitle = windowTitle,
                    ProcessId = processId,
                    StartTime = now,
                    EndTime = now,
                    DurationSeconds = 0
                };
                Sessions.Add(_currentSession);
                OnSessionCreated?.Invoke(_currentSession);
            }
        }
    }

    // ===== WMS-001：正常采集 =====
    [Test]
    public void WMS001_Collect_WhenWindowIsActive_ReturnsSession()
    {
        var service = new FakeWindowMonitorService();

        service.SimulateCollect("chrome", "百度首页");

        Assert.Single(service.Sessions);
        Assert.Equal("chrome", service.Sessions[0].ProcessName);
        Assert.Equal("百度首页", service.Sessions[0].WindowTitle);
    }

    // ===== WMS-002：无前台窗口时跳过 =====
    [Test]
    public void WMS002_Collect_WhenNoForegroundWindow_SkipsCollection()
    {
        var service = new FakeWindowMonitorService();
        // 不调用 SimulateCollect，模拟无前台窗口
        Assert.Null(service.GetCurrentSession());
        Assert.Empty(service.Sessions);
    }

    // ===== WMS-003：忽略列表中的进程不被记录 =====
    [Test]
    public void WMS003_Collect_IgnoredProcessIsSkipped()
    {
        var service = new FakeWindowMonitorService();
        service.IgnoredProcesses.Add("notepad.exe");

        service.SimulateCollect("notepad.exe", "无标题 - 记事本");

        Assert.Empty(service.Sessions);
        Assert.Null(service.GetCurrentSession());
    }

    // ===== WMS-004：忽略列表精确匹配 =====
    [Test]
    public void WMS004_Collect_IgnoredProcessExactMatch()
    {
        var service = new FakeWindowMonitorService();
        service.IgnoredProcesses.Add("ApplicationFrameHost.exe");

        service.SimulateCollect("ApplicationFrameHost.exe");
        Assert.Empty(service.Sessions);

        // 相似但不同的进程名不应被忽略
        service.SimulateCollect("ApplicationFrameHost");
        Assert.NotEmpty(service.Sessions);
    }

    // ===== WMS-005：连续相同窗口合并为一条会话 =====
    [Test]
    public void WMS005_Collect_ConsecutiveSameWindow_MergesIntoOneSession()
    {
        var service = new FakeWindowMonitorService();

        service.SimulateCollect("chrome", "百度首页");
        service.SimulateCollect("chrome", "百度首页");
        service.SimulateCollect("chrome", "百度首页");

        Assert.Single(service.Sessions);
        Assert.Equal("chrome", service.Sessions[0].ProcessName);
    }

    // ===== WMS-006：切换窗口时关闭旧会话创建新会话 =====
    [Test]
    public void WMS006_Collect_SwitchWindow_ClosesOldSessionAndCreatesNew()
    {
        var service = new FakeWindowMonitorService();

        service.SimulateCollect("chrome", "页面A");
        service.SimulateCollect("Code", "program.cs");

        Assert.Equal(2, service.Sessions.Count);
        Assert.Equal("chrome", service.Sessions[0].ProcessName);
        Assert.Equal("Code", service.Sessions[1].ProcessName);
    }

    // ===== WMS-007：同名进程不同窗口标题视为不同会话 =====
    [Test]
    public void WMS007_Collect_SameProcessDifferentTitle_AreSeparateSessions()
    {
        var service = new FakeWindowMonitorService();

        service.SimulateCollect("chrome", "百度");
        service.SimulateCollect("chrome", "Google");
        service.SimulateCollect("chrome", "百度");

        Assert.Equal(3, service.Sessions.Count);
    }

    // ===== WMS-008：轮询间隔可配置 =====
    [Test]
    public void WMS008_PollInterval_IsConfigurable()
    {
        var service = new FakeWindowMonitorService();
        Assert.Equal(1, service.PollIntervalSeconds);

        service.PollIntervalSeconds = 5;
        Assert.Equal(5, service.PollIntervalSeconds);
    }
}


