using ScreenMonitor.Core.Data;
using ScreenMonitor.Core.Models;
using ScreenMonitor.Core.Services;

namespace ScreenMonitor.Tests;

public class DataAggregationServiceTests
{
    private static (DataAggregationService, string) CreateService()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "screenmon_agg_" + System.Guid.NewGuid().ToString("N") + ".json");
        var repo = new JsonSessionRepository(path);
        return (new DataAggregationService(repo), path);
    }

    [Test]
    public async Task DAG001_CloseAllActiveSessions_ClosesEveryOpenSession()
    {
        var (service, path) = CreateService();
        try
        {
            await service.CloseAllActiveSessionsAsync();
        }
        finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    }

    [Test]
    public async Task DAG002_GenerateDailySummary_ReturnsCorrectSummary()
    {
        var (service, path) = CreateService();
        try
        {
            var date = new DateOnly(2026, 6, 12);
            var start = date.ToDateTime(TimeOnly.MinValue);
            var repoField = typeof(DataAggregationService).GetField("_repository",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var repo = repoField?.GetValue(service) as JsonSessionRepository;
            var sessionsField = typeof(JsonSessionRepository).GetField("_sessions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sessions = (System.Collections.Generic.List<ActivitySession>?)sessionsField?.GetValue(repo);
            sessions?.Add(new ActivitySession { Id = 1, ProcessName = "chrome", IsIdle = false,
                StartTime = start.AddHours(9), EndTime = start.AddHours(11), DurationSeconds = 7200 });
            sessions?.Add(new ActivitySession { Id = 2, ProcessName = "code", IsIdle = false,
                StartTime = start.AddHours(13), EndTime = start.AddHours(15), DurationSeconds = 7200 });

            var summary = await service.GenerateDailySummaryAsync(date);
            Assert.Equal(date, summary.Date);
            Assert.Equal(14400, summary.TotalActiveSeconds);
            Assert.Equal(2, summary.RecordCount);
        }
        finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    }

    [Test]
    public async Task DAG003_ExportToCsv_CreatesFile()
    {
        var (service, path) = CreateService();
        var csvPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "testcsv_" + System.Guid.NewGuid().ToString("N") + ".csv");
        try
        {
            await service.ExportToCsvAsync(csvPath);
            Assert.True(System.IO.File.Exists(csvPath), "CSV file should exist");
            var content = System.IO.File.ReadAllText(csvPath);
            Assert.True(content.Length > 0, "CSV file should not be empty");
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            if (System.IO.File.Exists(csvPath)) System.IO.File.Delete(csvPath);
        }
    }

    [Test]
    public async Task DAG004_ExportToJson_CreatesFile()
    {
        var (service, path) = CreateService();
        var jsonPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "testjson_" + System.Guid.NewGuid().ToString("N") + ".json");
        try
        {
            await service.ExportToJsonAsync(jsonPath);
            Assert.True(System.IO.File.Exists(jsonPath), "JSON file should exist");
            var content = System.IO.File.ReadAllText(jsonPath);
            Assert.True(content.Length > 0, "JSON file should not be empty");
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            if (System.IO.File.Exists(jsonPath)) System.IO.File.Delete(jsonPath);
        }
    }
}