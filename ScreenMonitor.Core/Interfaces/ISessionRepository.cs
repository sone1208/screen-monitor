using ScreenMonitor.Core.Models;

namespace ScreenMonitor.Core.Interfaces;

/// <summary>
/// 会话数据仓库接口
/// </summary>
public interface ISessionRepository
{
    Task<ActivitySession> AddAsync(ActivitySession session);
    Task<ActivitySession?> GetByIdAsync(long id);
    Task<List<ActivitySession>> GetByDateRangeAsync(DateOnly from, DateOnly to);
    Task<List<ActivitySession>> GetByApplicationAsync(int applicationId, DateOnly? from = null, DateOnly? to = null);
    Task UpdateAsync(ActivitySession session);
    Task DeleteAsync(long id);
    Task<List<(string ProcessName, long TotalSeconds, int Count)>> GetUsageSummaryAsync(DateOnly date);
    Task<int> CleanupOldDataAsync(DateTime cutoffDate);
}
