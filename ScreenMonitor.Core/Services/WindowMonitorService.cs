using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ScreenMonitor.Core.Interfaces;
using ScreenMonitor.Core.Models;
using ScreenMonitor.Core.Win32;

namespace ScreenMonitor.Core.Services;

/// <summary>
/// 窗口监控服务：轮询前台窗口，记录应用使用会话
/// </summary>
public class WindowMonitorService : IWindowMonitorService, IDisposable
{
    private Timer? _timer;
    private ActivitySession? _currentSession;
    private readonly ISessionRepository _repository;
    private readonly IIdleDetectionService _idleDetector;
    private string? _ignoreFilePath;

    public bool IsRunning { get; private set; }
    public List<string> IgnoredProcesses { get; set; } = new();
    public int PollIntervalSeconds { get; set; } = 1;

    public event Action<ActivitySession>? OnSessionCreated;
    public event Action<ActivitySession>? OnSessionUpdated;

    public WindowMonitorService(ISessionRepository repository, IIdleDetectionService idleDetector)
    {
        _repository = repository;
        _idleDetector = idleDetector;
    }

    public void SetIgnoreFilePath(string path)
    {
        _ignoreFilePath = path;
        LoadIgnoreList();
    }

    public void SaveIgnoreList()
    {
        if (_ignoreFilePath == null) return;
        try
        {
            var dir = System.IO.Path.GetDirectoryName(_ignoreFilePath);
            if (dir != null && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(IgnoredProcesses, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(_ignoreFilePath, json);
        }
        catch { }
    }

    public void LoadIgnoreList()
    {
        if (_ignoreFilePath == null || !System.IO.File.Exists(_ignoreFilePath)) return;
        try
        {
            var json = System.IO.File.ReadAllText(_ignoreFilePath);
            var list = JsonSerializer.Deserialize<List<string>>(json);
            if (list != null) IgnoredProcesses = list;
        }
        catch { }
    }

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        _timer = new Timer(CollectTick, null, 0, PollIntervalSeconds * 1000);
    }

    public void Stop()
    {
        IsRunning = false;
        _timer?.Dispose();
        _timer = null;
        CloseCurrentSessionAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public ActivitySession? GetCurrentSession() => _currentSession;

    private void CollectTick(object? state)
    {
        if (!IsRunning) return;

        try
        {
            bool isIdle = _idleDetector.IsUserIdle();
            var now = DateTime.Now;

            if (isIdle)
            {
                // 切换到空闲状态：关闭正在追踪的 app session
                if (_currentSession != null && !_currentSession.IsIdle)
                    CloseCurrentSessionAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                // 创建或更新空闲 session
                if (_currentSession == null || !_currentSession.IsIdle)
                {
                    _currentSession = new ActivitySession
                    {
                        ProcessName = "(空闲)",
                        ExecutablePath = "",
                        WindowTitle = "用户无操作",
                        ProcessId = 0,
                        StartTime = now,
                        EndTime = now,
                        DurationSeconds = 0,
                        IsIdle = true
                    };
                    _repository.AddAsync(_currentSession).ConfigureAwait(false).GetAwaiter().GetResult();
                    OnSessionCreated?.Invoke(_currentSession);
                }
                else
                {
                    _currentSession.EndTime = now;
                    _currentSession.DurationSeconds = (long)(now - _currentSession.StartTime).TotalSeconds;
                    _repository.UpdateAsync(_currentSession).ConfigureAwait(false).GetAwaiter().GetResult();
                    OnSessionUpdated?.Invoke(_currentSession);
                }
                return;
            }

            // 从空闲恢复：关闭空闲 session
            if (_currentSession != null && _currentSession.IsIdle)
                CloseCurrentSessionAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            var hWnd = NativeMethods.GetForegroundWindow();
            if (hWnd == IntPtr.Zero) return;

            if (!NativeMethods.IsWindowVisible(hWnd) || NativeMethods.IsIconic(hWnd))
            {
                if (_currentSession != null)
                    CloseCurrentSessionAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                return;
            }

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            if (processId == 0) return;

            var process = Process.GetProcessById((int)processId);
            var processName = process.ProcessName;
            var exePath = GetExecutablePath(process.Handle);
            var title = GetWindowText(hWnd);

            if (IgnoredProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase))
            {
                if (_currentSession != null)
                    CloseCurrentSessionAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                return;
            }

            // 合并同一进程的连续 session
            if (_currentSession != null &&
                _currentSession.ProcessName == processName &&
                _currentSession.WindowTitle == title)
            {
                _currentSession.EndTime = now;
                _currentSession.DurationSeconds = (long)(now - _currentSession.StartTime).TotalSeconds;
                OnSessionUpdated?.Invoke(_currentSession);
            }
            else
            {
                if (_currentSession != null)
                    CloseCurrentSessionAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                _currentSession = new ActivitySession
                {
                    ProcessName = processName,
                    ExecutablePath = exePath,
                    WindowTitle = title,
                    ProcessId = (int)processId,
                    StartTime = now,
                    EndTime = now,
                    DurationSeconds = 0
                };

                _repository.AddAsync(_currentSession).ConfigureAwait(false).GetAwaiter().GetResult();
                OnSessionCreated?.Invoke(_currentSession);
            }
        }
        catch
        {
        }
    }

    private async Task CloseCurrentSessionAsync()
    {
        if (_currentSession == null) return;

        _currentSession.EndTime = DateTime.Now;
        _currentSession.DurationSeconds = (long)(_currentSession.EndTime - _currentSession.StartTime).TotalSeconds;
        await _repository.UpdateAsync(_currentSession);

        var closed = _currentSession;
        _currentSession = null;
        OnSessionUpdated?.Invoke(closed);
    }

    private static string GetExecutablePath(IntPtr hProcess)
    {
        try
        {
            var sb = new StringBuilder(1024);
            uint size = 1024;
            if (NativeMethods.QueryFullProcessImageName(hProcess, 0, sb, ref size))
                return sb.ToString();
        }
        catch { }
        return "";
    }

    private static string GetWindowText(IntPtr hWnd)
    {
        var sb = new StringBuilder(512);
        NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public void Dispose()
    {
        Stop();
    }
}
