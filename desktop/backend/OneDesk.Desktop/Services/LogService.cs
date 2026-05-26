using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OneDesk.Desktop.Services;

/// <summary>
/// 日志服务
/// 分级日志系统（DEBUG, INFO, WARN, ERROR），SQLite 持久化
/// </summary>
public class LogService
{
    private readonly StorageService _storageService;
    private const int MaxLogEntries = 100000; // 最大日志条数
    private const int PruneBatchSize = 10000; // 清理时删除的条数

    public event EventHandler<LogEventArgs>? LogAdded;

    public LogService(StorageService storageService)
    {
        _storageService = storageService;
    }

    public void Debug(string source, string message, string? stackTrace = null, string? deviceId = null, string? pluginId = null)
        => Log("debug", source, message, stackTrace, deviceId, pluginId);

    public void Info(string source, string message, string? stackTrace = null, string? deviceId = null, string? pluginId = null)
        => Log("info", source, message, stackTrace, deviceId, pluginId);

    public void Warn(string source, string message, string? stackTrace = null, string? deviceId = null, string? pluginId = null)
        => Log("warn", source, message, stackTrace, deviceId, pluginId);

    public void Error(string source, string message, string? stackTrace = null, string? deviceId = null, string? pluginId = null)
        => Log("error", source, message, stackTrace, deviceId, pluginId);

    public void Log(string level, string source, string message, string? stackTrace = null, string? deviceId = null, string? pluginId = null)
    {
        var entry = new Models.LogEntry
        {
            Level = level,
            Source = source,
            Message = message,
            StackTrace = stackTrace,
            DeviceId = deviceId,
            PluginId = pluginId,
            Timestamp = DateTime.UtcNow
        };

        // 持久化到 SQLite（异步，不阻塞调用方）
        _ = _storageService.InsertLogAsync(entry);

        // 触发事件通知前端
        LogAdded?.Invoke(this, new LogEventArgs(entry));

        // 控制台输出
        var color = level switch
        {
            "debug" => ConsoleColor.Gray,
            "info" => ConsoleColor.White,
            "warn" => ConsoleColor.Yellow,
            "error" => ConsoleColor.Red,
            _ => ConsoleColor.White
        };

        Console.ForegroundColor = color;
        Console.WriteLine($"[{level.ToUpper(),5}] [{source}] {message}");
        Console.ResetColor();
    }

    /// <summary>
    /// 查询日志
    /// </summary>
    public async Task<LogQueryResult> QueryLogsAsync(LogQueryFilter filter)
    {
        return await _storageService.QueryLogsAsync(filter);
    }

    /// <summary>
    /// 清理过期日志
    /// </summary>
    public async Task PruneLogsAsync()
    {
        await _storageService.PruneLogsAsync(MaxLogEntries, PruneBatchSize);
    }
}

public class LogEventArgs : EventArgs
{
    public Models.LogEntry Entry { get; }
    public LogEventArgs(Models.LogEntry entry) => Entry = entry;
}

public class LogQueryFilter
{
    public string? Level { get; set; }
    public string? Source { get; set; }
    public string? SearchText { get; set; }
    public string? DeviceId { get; set; }
    public string? PluginId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; } = 100;
}

public class LogQueryResult
{
    public List<Models.LogEntry> Entries { get; set; } = [];
    public int Total { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
}
