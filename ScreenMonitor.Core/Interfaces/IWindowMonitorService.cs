using ScreenMonitor.Core.Models;

namespace ScreenMonitor.Core.Interfaces;

/// <summary>
/// 窗口监控服务接口
/// </summary>
public interface IWindowMonitorService
{
    /// <summary>开始监控</summary>
    void Start();

    /// <summary>停止监控</summary>
    void Stop();

    /// <summary>当前是否正在监控</summary>
    bool IsRunning { get; }

    /// <summary>获取当前前台活动会话（可能为 null）</summary>
    ActivitySession? GetCurrentSession();

    /// <summary>获取或设置忽略的进程名列表</summary>
    List<string> IgnoredProcesses { get; set; }

    /// <summary>获取或设置轮询间隔（秒）</summary>
    int PollIntervalSeconds { get; set; }

    /// <summary>当有新会话创建时触发</summary>
    event Action<ActivitySession>? OnSessionCreated;

    /// <summary>当会话更新时触发</summary>
    event Action<ActivitySession>? OnSessionUpdated;
}
