namespace ScreenMonitor.Core.Models;

/// <summary>
/// 应用程序信息
/// </summary>
public class ApplicationInfo
{
    public int Id { get; set; }
    public string ProcessName { get; set; } = "";
    public string? ExecutablePath { get; set; }
    public string? DisplayName { get; set; }
    public byte[]? Icon { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public bool IsIgnored { get; set; }
}
