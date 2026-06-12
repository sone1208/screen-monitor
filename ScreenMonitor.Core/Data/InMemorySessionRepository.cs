using ScreenMonitor.Core.Models;
using ScreenMonitor.Core.Interfaces;

namespace ScreenMonitor.Core.Data;

/// <summary>
/// 内存版会话仓库（测试用，无需数据库）
/// </summary>
public class InMemorySessionRepository : ISessionRepository
{
    private readonly List<ActivitySession> _sessions = new();
    private readonly List<(string ProcessName, long TotalSeconds, int Count)> _usageSummary = new();
    private long _nextId = 1;

    public void SetUsageSummary(List<(string ProcessName, long TotalSeconds, int Count)> summary)
    {
        _usageSummary.Clear();
        _usageSummary.AddRange(summary);
    }

    public Task<ActivitySession> AddAsync(ActivitySession session)
    {
        session.Id = _nextId++;
        session.CreatedAt = DateTime.UtcNow;
        _sessions.Add(session);
        return Task.FromResult(session);
    }

    public Task<ActivitySession?> GetByIdAsync(long id)
    {
        var result = _sessions.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(result);
    }

    public Task<List<ActivitySession>> GetByDateRangeAsync(DateOnly from, DateOnly to)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);
        var result = _sessions
            .Where(s => s.StartTime >= fromDt && s.StartTime <= toDt)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<List<ActivitySession>> GetByApplicationAsync(int applicationId, DateOnly? from = null, DateOnly? to = null)
    {
        var query = _sessions.Where(s => s.ApplicationId == applicationId).AsEnumerable();
        if (from.HasValue)
            query = query.Where(s => s.StartTime >= from.Value.ToDateTime(TimeOnly.MinValue));
        if (to.HasValue)
            query = query.Where(s => s.StartTime <= to.Value.ToDateTime(TimeOnly.MaxValue));
        return Task.FromResult(query.ToList());
    }

    public Task UpdateAsync(ActivitySession session)
    {
        var index = _sessions.FindIndex(s => s.Id == session.Id);
        if (index >= 0)
            _sessions[index] = session;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(long id)
    {
        _sessions.RemoveAll(s => s.Id == id);
        return Task.CompletedTask;
    }

    public Task<List<(string ProcessName, long TotalSeconds, int Count)>> GetUsageSummaryAsync(DateOnly date)
    {
        return Task.FromResult(_usageSummary.ToList());
    }

    public Task<int> CleanupOldDataAsync(DateTime cutoffDate)
    {
        var count = _sessions.RemoveAll(s => s.CreatedAt < cutoffDate);
        return Task.FromResult(count);
    }

    /// <summary>直接添加已构造好的记录（测试用，不自动设置 CreatedAt）</summary>
    public Task AddRawAsync(ActivitySession session)
    {
        _sessions.Add(session);
        return Task.CompletedTask;
    }
    public int SessionCount => _sessions.Count;
}

