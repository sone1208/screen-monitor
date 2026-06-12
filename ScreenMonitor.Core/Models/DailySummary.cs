namespace ScreenMonitor.Core.Models;

/// <summary>
/// 每日汇总快照
/// </summary>
public class DailySummary
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public long TotalActiveSeconds { get; set; }
    public long TotalIdleSeconds { get; set; }
    public int RecordCount { get; set; }
}
