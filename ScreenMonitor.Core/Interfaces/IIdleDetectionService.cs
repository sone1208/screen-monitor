namespace ScreenMonitor.Core.Interfaces;

/// <summary>
/// 空闲检测服务接口
/// </summary>
public interface IIdleDetectionService
{
    /// <summary>用户是否处于空闲状态</summary>
    bool IsUserIdle();

    /// <summary>获取空闲阈值（秒）</summary>
    int IdleThresholdSeconds { get; }

    /// <summary>设置空闲阈值（秒）</summary>
    void SetIdleThreshold(int seconds);

    /// <summary>获取上次用户输入以来的空闲秒数</summary>
    int GetIdleSeconds();
}
