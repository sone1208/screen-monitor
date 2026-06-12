using ScreenMonitor.Core.Data;
using ScreenMonitor.Core.Models;

namespace ScreenMonitor.Tests;

public class SessionRepositoryTests
{
    // ===== REP-001：创建并查询会话 =====
    [Fact]
    public async Task REP001_AddAndGetById_ReturnsSameSession()
    {
        var repo = new InMemorySessionRepository();
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

    // ===== REP-002：按日期范围查询 =====
    [Fact]
    public async Task REP002_GetByDateRange_ReturnsCorrectRecords()
    {
        var repo = new InMemorySessionRepository();

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
        await repo.AddAsync(new ActivitySession
        {
            ProcessName = "wechat",
            StartTime = new DateTime(2026, 5, 30, 9, 0, 0),
            EndTime = new DateTime(2026, 5, 30, 10, 0, 0),
            DurationSeconds = 3600
        });

        var result = await repo.GetByDateRangeAsync(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 15));

        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.ProcessName == "chrome");
        Assert.Contains(result, s => s.ProcessName == "code");
    }

    // ===== REP-003：按应用查询汇总 =====
    [Fact]
    public async Task REP003_GetUsageSummary_ReturnsCorrectAggregation()
    {
        var repo = new InMemorySessionRepository();
        var date = new DateOnly(2026, 6, 12);

        repo.SetUsageSummary(new List<(string, long, int)>
        {
            ("chrome", 7200, 3),
            ("code", 3600, 2)
        });

        var summary = await repo.GetUsageSummaryAsync(date);

        Assert.Equal(2, summary.Count);
        Assert.Equal(("chrome", 7200, 3), summary[0]);
        Assert.Equal(("code", 3600, 2), summary[1]);
    }

    // ===== REP-004：自动清理过期数据 =====
    [Fact]
    public async Task REP004_CleanupOldData_RemovesExpiredRecords()
    {
        var repo = new InMemorySessionRepository();

        // 直接操作内部数据，绕过 AddAsync 的自动 CreatedAt
        var oldSession = new ActivitySession
        {
            Id = 1,
            ProcessName = "old_app",
            StartTime = DateTime.Now.AddDays(-40),
            EndTime = DateTime.Now.AddDays(-40),
            DurationSeconds = 3600,
            CreatedAt = DateTime.UtcNow.AddDays(-40)
        };
        await repo.AddRawAsync(oldSession);

        var newSession = new ActivitySession
        {
            Id = 2,
            ProcessName = "new_app",
            StartTime = DateTime.Now.AddDays(-5),
            EndTime = DateTime.Now,
            DurationSeconds = 3600,
            CreatedAt = DateTime.UtcNow
        };
        await repo.AddRawAsync(newSession);

        var cutoff = DateTime.UtcNow.AddDays(-30);
        var deleted = await repo.CleanupOldDataAsync(cutoff);

        Assert.Equal(1, deleted);
    }

    // ===== REP-005：更新会话 =====
    [Fact]
    public async Task REP005_UpdateSession_ModifiesCorrectly()
    {
        var repo = new InMemorySessionRepository();
        var session = new ActivitySession
        {
            ProcessName = "chrome",
            StartTime = DateTime.Now.AddHours(-1),
            EndTime = DateTime.Now.AddHours(-1),
            DurationSeconds = 0
        };

        var created = await repo.AddAsync(session);

        // 更新时长
        created.EndTime = DateTime.Now;
        created.DurationSeconds = 3600;
        await repo.UpdateAsync(created);

        var fetched = await repo.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(3600, fetched!.DurationSeconds);
    }

    // ===== REP-006：删除会话 =====
    [Fact]
    public async Task REP006_DeleteSession_RemovesFromStore()
    {
        var repo = new InMemorySessionRepository();
        var session = new ActivitySession { ProcessName = "temp" };
        var created = await repo.AddAsync(session);

        await repo.DeleteAsync(created.Id);

        var fetched = await repo.GetByIdAsync(created.Id);
        Assert.Null(fetched);
    }
}

