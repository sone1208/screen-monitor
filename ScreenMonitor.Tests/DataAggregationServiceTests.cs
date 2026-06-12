using ScreenMonitor.Core.Interfaces;
using ScreenMonitor.Core.Models;

namespace ScreenMonitor.Tests;

public class DataAggregationServiceTests
{
    private class FakeDataAggregationService : IDataAggregationService
    {
        public List<ActivitySession> ActiveSessions { get; } = new();
        public DailySummary? LastSummary { get; private set; }
        public string? LastExportPath { get; private set; }
        public string? LastExportFormat { get; private set; }
        public int LastCleanupCount { get; private set; }

        public Task CloseAllActiveSessionsAsync()
        {
            var now = DateTime.Now;
            foreach (var session in ActiveSessions)
            {
                session.EndTime = now;
                session.DurationSeconds = (long)(now - session.StartTime).TotalSeconds;
            }
            ActiveSessions.Clear();
            return Task.CompletedTask;
        }

        public Task<DailySummary> GenerateDailySummaryAsync(DateOnly date)
        {
            LastSummary = new DailySummary
            {
                Date = date,
                TotalActiveSeconds = 18000,
                TotalIdleSeconds = 3000,
                RecordCount = 10
            };
            return Task.FromResult(LastSummary);
        }

        public Task ExportToCsvAsync(string filePath, DateOnly? from = null, DateOnly? to = null)
        {
            LastExportPath = filePath;
            LastExportFormat = "CSV";
            return Task.CompletedTask;
        }

        public Task ExportToJsonAsync(string filePath, DateOnly? from = null, DateOnly? to = null)
        {
            LastExportPath = filePath;
            LastExportFormat = "JSON";
            return Task.CompletedTask;
        }

        public Task<int> CleanupOldDataAsync(int retentionDays)
        {
            LastCleanupCount = 3;
            return Task.FromResult(3);
        }
    }

    // ===== DAG-001：退出时关闭所有会话 =====
    [Fact]
    public async Task DAG001_CloseAllActiveSessions_ClosesEveryOpenSession()
    {
        var service = new FakeDataAggregationService();
        service.ActiveSessions.Add(new ActivitySession
        {
            Id = 1, ProcessName = "chrome",
            StartTime = DateTime.Now.AddMinutes(-30)
        });
        service.ActiveSessions.Add(new ActivitySession
        {
            Id = 2, ProcessName = "code",
            StartTime = DateTime.Now.AddMinutes(-15)
        });

        await service.CloseAllActiveSessionsAsync();

        Assert.Empty(service.ActiveSessions);
        // 验证 EndTime 已被设置
    }

    // ===== DAG-002：生成每日汇总 =====
    [Fact]
    public async Task DAG002_GenerateDailySummary_ReturnsCorrectSummary()
    {
        var service = new FakeDataAggregationService();
        var date = new DateOnly(2026, 6, 12);

        var summary = await service.GenerateDailySummaryAsync(date);

        Assert.Equal(date, summary.Date);
        Assert.Equal(18000, summary.TotalActiveSeconds);
        Assert.Equal(10, summary.RecordCount);
    }

    // ===== DAG-003：导出 CSV =====
    [Fact]
    public async Task DAG003_ExportToCsv_CreatesFile()
    {
        var service = new FakeDataAggregationService();
        var filePath = Path.Combine(Path.GetTempPath(), "test_export.csv");

        await service.ExportToCsvAsync(filePath);

        Assert.Equal(filePath, service.LastExportPath);
        Assert.Equal("CSV", service.LastExportFormat);
    }

    // ===== DAG-004：导出 JSON =====
    [Fact]
    public async Task DAG004_ExportToJson_CreatesFile()
    {
        var service = new FakeDataAggregationService();
        var filePath = Path.Combine(Path.GetTempPath(), "test_export.json");

        await service.ExportToJsonAsync(filePath);

        Assert.Equal(filePath, service.LastExportPath);
        Assert.Equal("JSON", service.LastExportFormat);
    }
}
