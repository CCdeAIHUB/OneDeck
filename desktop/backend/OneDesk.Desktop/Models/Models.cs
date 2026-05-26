using System;
using System.Collections.Generic;

namespace OneDeck.Desktop.Models;

/// <summary>
/// 已连接的移动端设备信息
/// </summary>
public class ConnectedDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // android, ios, harmony
    public string OsVersion { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    public string? AssignedSchemeId { get; set; }
    public bool IsOnline { get; set; } = true;
}

/// <summary>
/// 方案定义
/// </summary>
public class Scheme
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TargetDeviceId { get; set; } = string.Empty;
    public SchemeLayout Layout { get; set; } = new();
    public List<SchemePluginInstance> Plugins { get; set; } = [];
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SchemeLayout
{
    public string Type { get; set; } = "grid"; // grid, free, tabs
    public int Columns { get; set; } = 4;
    public int Rows { get; set; } = 3;
    public List<SchemePage> Pages { get; set; } = [];
}

public class SchemePage
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<SchemeSlot> Slots { get; set; } = [];
}

public class SchemeSlot
{
    public string Id { get; set; } = string.Empty;
    public string PluginId { get; set; } = string.Empty;
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; } = 1;
    public int ColumnSpan { get; set; } = 1;
}

public class SchemePluginInstance
{
    public string PluginId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public Dictionary<string, object> Config { get; set; } = new();
}

/// <summary>
/// 插件信息
/// </summary>
public class PluginInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string EntryPath { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;   // 编译后的完整JS模块
    public string ScopedCss { get; set; } = string.Empty;    // Scoped CSS
    public bool IsEnabled { get; set; } = true;
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 日志条目
/// </summary>
public class LogEntry
{
    public long Id { get; set; }
    public string Level { get; set; } = "info";
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? DeviceId { get; set; }
    public string? PluginId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// WebSocket 消息信封
/// </summary>
public class WsMessage
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public string DeviceId { get; set; } = string.Empty;
    public string? PluginId { get; set; }
    public Dictionary<string, object> Payload { get; set; } = new();
}
