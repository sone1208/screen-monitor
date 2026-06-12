using System.Text.Json;
using ScreenMonitor.Core.Interfaces;
using ScreenMonitor.Core.Models;

namespace ScreenMonitor.Core.Data;

/// <summary>
/// 基于 JSON 文件的会话数据仓库（零外部依赖，无需数据库）
/// </summary>
public class JsonSessionRepository : ISessionRepository
{
    private readonly string _filePath;
    private List<ActivitySession> _sessions = new();
    private long _nextId = 1;
    private readonly object _lock = new();

    public JsonSessionRepository(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "data.json");
        Load();
    }

    private void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
            {
                _sessions = new List<ActivitySession>();
                _nextId = 1;
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath, System.Text.Encoding.UTF8);
                _sessions = JsonSerializer.Deserialize<List<ActivitySession>>(json) ?? new();
                _nextId = _sessions.Count > 0 ? _sessions.Max(s => s.Id) + 1 : 1;
            }
            catch
            {
                _sessions = new List<ActivitySession>();
                _nextId = 1;
            }
        }
    }

    private void Save()
    {
        lock (_lock)
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_sessions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json, System.Text.Encoding.UTF8);
        }
    }

    public Task<ActivitySession> AddAsync(ActivitySession session)
    {
        lock (_lock)
        {
            session.Id = _nextId++;
            session.CreatedAt = DateTime.UtcNow;
            _sessions.Add(session);
            Save();
        }
        return Task.FromResult(session);
    }

    public Task<ActivitySession?> GetByIdAsync(long id)
    {
        lock (_lock)
        {
            return Task.FromResult(_sessions.FirstOrDefault(s => s.Id == id));
        }
    }

    public Task<List<ActivitySession>> GetByDateRangeAsync(DateOnly from, DateOnly to)
    {
        lock (_lock)
        {
            var fromDt = from.ToDateTime(TimeOnly.MinValue);
            var toDt = to.ToDateTime(TimeOnly.MaxValue);
            return Task.FromResult(
                _sessions.Where(s => s.StartTime >= fromDt && s.StartTime <= toDt).ToList());
        }
    }

    public Task<List<ActivitySession>> GetByApplicationAsync(int applicationId, DateOnly? from = null, DateOnly? to = null)
    {
        lock (_lock)
        {
            var query = _sessions.Where(s => s.ApplicationId == applicationId).AsEnumerable();
            if (from.HasValue)
                query = query.Where(s => s.StartTime >= from.Value.ToDateTime(TimeOnly.MinValue));
            if (to.HasValue)
                query = query.Where(s => s.StartTime <= to.Value.ToDateTime(TimeOnly.MaxValue));
            return Task.FromResult(query.ToList());
        }
    }

    public Task UpdateAsync(ActivitySession session)
    {
        lock (_lock)
        {
            var index = _sessions.FindIndex(s => s.Id == session.Id);
            if (index >= 0)
            {
                _sessions[index] = session;
                Save();
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(long id)
    {
        lock (_lock)
        {
            _sessions.RemoveAll(s => s.Id == id);
            Save();
        }
        return Task.CompletedTask;
    }

    public Task<List<(string ProcessName, long TotalSeconds, int Count)>> GetUsageSummaryAsync(DateOnly date)
    {
        lock (_lock)
        {
            var fromDt = date.ToDateTime(TimeOnly.MinValue);
            var toDt = date.ToDateTime(TimeOnly.MaxValue);

            var result = _sessions
                .Where(s => s.StartTime >= fromDt && s.StartTime <= toDt && !s.IsIdle)
                .GroupBy(s => s.ProcessName)
                .Select(g => (
                    g.Key,
                    (long)g.Sum(s => s.DurationSeconds),
                    g.Count()))
                .OrderByDescending(x => x.Item2)
                .ToList();

            return Task.FromResult(result);
        }
    }

    public Task<int> CleanupOldDataAsync(DateTime cutoffDate)
    {
        int count;
        lock (_lock)
        {
            count = _sessions.RemoveAll(s => s.CreatedAt < cutoffDate);
            if (count > 0) Save();
        }
        return Task.FromResult(count);
    }
}
