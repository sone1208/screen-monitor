using ScreenMonitor.Core.Interfaces;
using ScreenMonitor.Core.Win32;

namespace ScreenMonitor.Core.Services;

/// <summary>
/// 空闲检测服务：通过 GetLastInputInfo 判断用户是否离开
/// </summary>
public class IdleDetectionService : IIdleDetectionService
{
    public int IdleThresholdSeconds { get; private set; } = 300;

    public IdleDetectionService(int idleThresholdSeconds = 300)
    {
        IdleThresholdSeconds = idleThresholdSeconds;
    }

    public void SetIdleThreshold(int seconds)
    {
        IdleThresholdSeconds = seconds > 0 ? seconds : 300;
    }

    public bool IsUserIdle()
    {
        return GetIdleSeconds() >= IdleThresholdSeconds;
    }

    public int GetIdleSeconds()
    {
        var plii = new NativeMethods.LASTINPUTINFO();
        plii.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(plii);

        if (NativeMethods.GetLastInputInfo(ref plii))
        {
            var tickCount = (uint)Environment.TickCount;
            var idleMs = tickCount - plii.dwTime;
            return (int)(idleMs / 1000);
        }

        return 0;
    }
}
