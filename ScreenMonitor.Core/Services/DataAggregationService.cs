using System.Text.Json;
using ScreenMonitor.Core.Interfaces;
using ScreenMonitor.Core.Models;

namespace ScreenMonitor.Core.Services;

/// <summary>
/// 数据聚合服务：会话管理、导出、清理
/// </summary>
public class DataAggregationService : IDataAggregationService
{
    private readonly ISessionRepository _repository;

    public DataAggregationService(ISessionRepository repository)
    {
        _repository = repository;
    }

    public async Task CloseAllActiveSessionsAsync()
    {
        // 关闭所有未结束的会话（EndTime == StartTime 的即为活跃会话）
        var today = DateOnly.FromDateTime(DateTime.Now);
        var sessions = await _repository.GetByDateRangeAsync(today, today);
        var now = DateTime.Now;

        foreach (var session in sessions)
        {
            if (session.EndTime == session.StartTime)
            {
                session.EndTime = now;
                session.DurationSeconds = (long)(now - session.StartTime).TotalSeconds;
                await _repository.UpdateAsync(session);
            }
        }
    }

    public async Task<DailySummary> GenerateDailySummaryAsync(DateOnly date)
    {
        var sessions = await _repository.GetByDateRangeAsync(date, date);
        var totalActive = sessions.Where(s => !s.IsIdle).Sum(s => s.DurationSeconds);
        var totalIdle = sessions.Where(s => s.IsIdle).Sum(s => s.DurationSeconds);

        return new DailySummary
        {
            Date = date,
            TotalActiveSeconds = totalActive,
            TotalIdleSeconds = totalIdle,
            RecordCount = sessions.Count
        };
    }

    public async Task ExportToCsvAsync(string filePath, DateOnly? from = null, DateOnly? to = null)
    {
        from ??= DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
        to ??= DateOnly.FromDateTime(DateTime.Now);

        var sessions = await _repository.GetByDateRangeAsync(from.Value, to.Value);

        using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
        await writer.WriteLineAsync("进程名,窗口标题,开始时间,结束时间,持续秒数,是否空闲");

        foreach (var s in sessions)
        {
            await writer.WriteLineAsync(
                $"{EscapeCsv(s.ProcessName)},{EscapeCsv(s.WindowTitle ?? "")}," +
                $"{s.StartTime:yyyy-MM-dd HH:mm:ss},{s.EndTime:yyyy-MM-dd HH:mm:ss}," +
                $"{s.DurationSeconds},{s.IsIdle}");
        }
    }

    public async Task ExportToJsonAsync(string filePath, DateOnly? from = null, DateOnly? to = null)
    {
        from ??= DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
        to ??= DateOnly.FromDateTime(DateTime.Now);

        var sessions = await _repository.GetByDateRangeAsync(from.Value, to.Value);

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(sessions, options);
        await File.WriteAllTextAsync(filePath, json, System.Text.Encoding.UTF8);
    }

    public async Task<int> CleanupOldDataAsync(int retentionDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        return await _repository.CleanupOldDataAsync(cutoff);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
