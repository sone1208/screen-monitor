namespace ScreenMonitor.Core.Models;

/// <summary>
/// 应用使用会话记录
/// </summary>
public class ActivitySession
{
    public long Id { get; set; }
    public int ApplicationId { get; set; }
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = "";
    public string? ExecutablePath { get; set; }
    public string? WindowTitle { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long DurationSeconds { get; set; }
    public bool IsIdle { get; set; }
    public DateTime CreatedAt { get; set; }
}
