using ScreenMonitor.Core.Models;

namespace ScreenMonitor.Core.Interfaces;

/// <summary>
/// 数据聚合服务接口
/// </summary>
public interface IDataAggregationService
{
    /// <summary>关闭所有未关闭的会话</summary>
    Task CloseAllActiveSessionsAsync();

    /// <summary>生成指定日期的汇总快照</summary>
    Task<DailySummary> GenerateDailySummaryAsync(DateOnly date);

    /// <summary>导出数据为 CSV</summary>
    Task ExportToCsvAsync(string filePath, DateOnly? from = null, DateOnly? to = null);

    /// <summary>导出数据为 JSON</summary>
    Task ExportToJsonAsync(string filePath, DateOnly? from = null, DateOnly? to = null);

    /// <summary>清理过期数据</summary>
    Task<int> CleanupOldDataAsync(int retentionDays);
}
