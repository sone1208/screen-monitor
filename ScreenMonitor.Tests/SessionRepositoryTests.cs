using ScreenMonitor.Core.Data;
using ScreenMonitor.Core.Models;

namespace ScreenMonitor.Tests;

public class SessionRepositoryTests
{
    private static string GetTempPath() =>
        Path.Combine(Path.GetTempPath(), $"screenmon_test_{Guid.NewGuid():N}.json");

    // ===== REP-001：创建并查询会话 =====
    [Test]
    public async Task REP001_AddAndGetById_ReturnsSameSession()
    {
        var path = GetTempPath();
        var repo = new JsonSessionRepository(path);
        try
        {
            var session = new ActivitySession
            {
                ProcessName = "chrome",
                WindowTitle = "百度首页",
                StartTime = DateTime.Now.AddMinutes(-10),
                EndTime = DateTime.Now,
                DurationSeconds = 600,
                ProcessId = 1234
            };

            var created = await repo.AddAsync(session);
            var fetched = await repo.GetByIdAsync(created.Id);

            Assert.NotNull(fetched);
            Assert.Equal("chrome", fetched!.ProcessName);
            Assert.Equal("百度首页", fetched.WindowTitle);
            Assert.Equal(600, fetched.DurationSeconds);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    // ===== REP-002：按日期范围查询 =====
    [Test]
    public async Task REP002_GetByDateRange_ReturnsCorrectRecords()
    {
        var path = GetTempPath();
        var repo = new JsonSessionRepository(path);
        try
        {
            await repo.AddAsync(new ActivitySession
            {
                ProcessName = "chrome",
                StartTime = new DateTime(2026, 6, 10, 10, 0, 0),
                EndTime = new DateTime(2026, 6, 10, 11, 0, 0),
                DurationSeconds = 3600
            });
            await repo.AddAsync(new ActivitySession
            {
                ProcessName = "code",
                StartTime = new DateTime(2026, 6, 12, 10, 0, 0),
                EndTime = new DateTime(2026, 6, 12, 12, 0, 0),
                DurationSeconds = 7200
            });

            var result = await repo.GetByDateRangeAsync(
                new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 15));

            Assert.Equal(2, result.Count);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    // ===== REP-003：按应用查询汇总 =====
    [Test]
    public async Task REP003_GetUsageSummary_ReturnsCorrectAggregation()
    {
        var path = GetTempPath();
        var repo = new JsonSessionRepository(path);
        try
        {
            var date = new DateOnly(2026, 6, 12);
            var start = date.ToDateTime(TimeOnly.MinValue);

            await repo.AddAsync(new ActivitySession
            {
                ProcessName = "chrome", StartTime = start.AddHours(9),
                EndTime = start.AddHours(11), DurationSeconds = 7200
            });
            await repo.AddAsync(new ActivitySession
            {
                ProcessName = "code", StartTime = start.AddHours(13),
                EndTime = start.AddHours(14), DurationSeconds = 3600
            });

            var summary = await repo.GetUsageSummaryAsync(date);

            Assert.Contains<(string ProcessName, long TotalSeconds, int Count)>(summary, s => s.ProcessName == "chrome" && s.TotalSeconds == 7200);
            Assert.Contains<(string ProcessName, long TotalSeconds, int Count)>(summary, s => s.ProcessName == "code" && s.TotalSeconds == 3600);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    // ===== REP-004：清理过期数据 =====
    [Test]
    public async Task REP004_CleanupOldData_RemovesExpiredRecords()
    {
        var path = GetTempPath();
        var repo = new JsonSessionRepository(path);
        try
        {
            var oldSession = new ActivitySession
            {
                ProcessName = "old_app",
                StartTime = DateTime.Now.AddDays(-40),
                EndTime = DateTime.Now.AddDays(-40),
                DurationSeconds = 3600,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            };
            // 直接操作文件来添加历史数据
            var field = typeof(JsonSessionRepository).GetField("_sessions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sessions = (List<ActivitySession>?)field?.GetValue(repo);
            sessions?.Add(oldSession);

            var newSession = new ActivitySession
            {
                ProcessName = "new_app",
                StartTime = DateTime.Now.AddDays(-5),
                EndTime = DateTime.Now,
                DurationSeconds = 3600,
                CreatedAt = DateTime.UtcNow
            };
            await repo.AddAsync(newSession);

            var cutoff = DateTime.UtcNow.AddDays(-30);
            var deleted = await repo.CleanupOldDataAsync(cutoff);

            Assert.Equal(1, deleted);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    // ===== REP-005：更新会话 =====
    [Test]
    public async Task REP005_UpdateSession_ModifiesCorrectly()
    {
        var path = GetTempPath();
        var repo = new JsonSessionRepository(path);
        try
        {
            var session = new ActivitySession
            {
                ProcessName = "chrome",
                StartTime = DateTime.Now.AddHours(-1),
                EndTime = DateTime.Now.AddHours(-1),
                DurationSeconds = 0
            };
            var created = await repo.AddAsync(session);

            created.EndTime = DateTime.Now;
            created.DurationSeconds = 3600;
            await repo.UpdateAsync(created);

            var fetched = await repo.GetByIdAsync(created.Id);
            Assert.NotNull(fetched);
            Assert.Equal(3600, fetched!.DurationSeconds);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    // ===== REP-006：删除会话 =====
    [Test]
    public async Task REP006_DeleteSession_RemovesFromStore()
    {
        var path = GetTempPath();
        var repo = new JsonSessionRepository(path);
        try
        {
            var session = new ActivitySession { ProcessName = "temp" };
            var created = await repo.AddAsync(session);

            await repo.DeleteAsync(created.Id);

            var fetched = await repo.GetByIdAsync(created.Id);
            Assert.Null(fetched);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    // ===== REP-007：数据持久化到文件 =====
    [Test]
    public async Task REP007_DataPersistsAcrossInstances()
    {
        var path = GetTempPath();
        try
        {
            var repo1 = new JsonSessionRepository(path);
            await repo1.AddAsync(new ActivitySession
            {
                ProcessName = "persist_test",
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                DurationSeconds = 100
            });

            var repo2 = new JsonSessionRepository(path);
            var sessions = await repo2.GetByDateRangeAsync(
                DateOnly.FromDateTime(DateTime.Now.AddDays(-1)),
                DateOnly.FromDateTime(DateTime.Now.AddDays(1)));

            Assert.Contains(sessions, (ActivitySession s) => s.ProcessName == "persist_test");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}



