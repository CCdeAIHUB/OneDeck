using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OneDeck.Desktop.Models;
using Microsoft.Data.Sqlite;

namespace OneDeck.Desktop.Services;

/// <summary>
/// 存储服务
/// SQLite 持久化存储，管理插件数据、方案配置、日志
/// 插件数据通过 plugin_id 进行隔离
/// </summary>
public class StorageService
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public StorageService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "OneDeck");
        Directory.CreateDirectory(dir);
        _dbPath = Path.Combine(dir, "onedeck.db");
    }

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        await _connection.OpenAsync();

        await CreateTablesAsync();
    }

    public async Task CloseAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }

    private async Task CreateTablesAsync()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS plugins (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                version TEXT NOT NULL,
                description TEXT DEFAULT '',
                author TEXT DEFAULT '',
                icon TEXT DEFAULT '',
                entry_path TEXT DEFAULT '',
                module_code TEXT DEFAULT '',
                scoped_css TEXT DEFAULT '',
                is_enabled INTEGER DEFAULT 1,
                installed_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS plugin_data (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                plugin_id TEXT NOT NULL,
                key TEXT NOT NULL,
                value TEXT,
                created_at TEXT DEFAULT (datetime('now')),
                updated_at TEXT DEFAULT (datetime('now')),
                UNIQUE(plugin_id, key)
            );

            CREATE TABLE IF NOT EXISTS schemes (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                target_device_id TEXT DEFAULT '',
                layout TEXT DEFAULT '{}',
                plugins TEXT DEFAULT '[]',
                version INTEGER DEFAULT 1,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS devices (
                device_id TEXT PRIMARY KEY,
                device_name TEXT DEFAULT '',
                platform TEXT DEFAULT '',
                os_version TEXT DEFAULT '',
                app_id TEXT DEFAULT '',
                screen_width INTEGER DEFAULT 0,
                screen_height INTEGER DEFAULT 0,
                assigned_scheme_id TEXT DEFAULT '',
                connected_at TEXT,
                last_heartbeat TEXT
            );

            CREATE TABLE IF NOT EXISTS logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                level TEXT NOT NULL DEFAULT 'info',
                message TEXT NOT NULL,
                source TEXT DEFAULT '',
                stack_trace TEXT DEFAULT '',
                device_id TEXT DEFAULT '',
                plugin_id TEXT DEFAULT '',
                timestamp TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_logs_level ON logs(level);
            CREATE INDEX IF NOT EXISTS idx_logs_source ON logs(source);
            CREATE INDEX IF NOT EXISTS idx_logs_timestamp ON logs(timestamp);
            CREATE INDEX IF NOT EXISTS idx_logs_device_id ON logs(device_id);
            CREATE INDEX IF NOT EXISTS idx_logs_plugin_id ON logs(plugin_id);

            CREATE TABLE IF NOT EXISTS temp_data (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                plugin_id TEXT NOT NULL,
                key TEXT NOT NULL,
                value TEXT,
                UNIQUE(plugin_id, key)
            );
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    // ==================== 插件数据操作 ====================

    public async Task<object?> GetPluginDataAsync(string pluginId, string key)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM plugin_data WHERE plugin_id = @pluginId AND key = @key";
        cmd.Parameters.AddWithValue("@pluginId", pluginId);
        cmd.Parameters.AddWithValue("@key", key);

        var result = await cmd.ExecuteScalarAsync();
        return result;
    }

    public async Task SetPluginDataAsync(string pluginId, string key, string value)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO plugin_data (plugin_id, key, value, updated_at)
            VALUES (@pluginId, @key, @value, datetime('now'))
            ON CONFLICT(plugin_id, key) DO UPDATE SET value = @value, updated_at = datetime('now')";
        cmd.Parameters.AddWithValue("@pluginId", pluginId);
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task RemovePluginDataAsync(string pluginId, string key)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM plugin_data WHERE plugin_id = @pluginId AND key = @key";
        cmd.Parameters.AddWithValue("@pluginId", pluginId);
        cmd.Parameters.AddWithValue("@key", key);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<string>> GetPluginDataKeysAsync(string pluginId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT key FROM plugin_data WHERE plugin_id = @pluginId";
        cmd.Parameters.AddWithValue("@pluginId", pluginId);

        var keys = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            keys.Add(reader.GetString(0));
        }
        return keys;
    }

    public async Task ClearPluginDataAsync(string pluginId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM plugin_data WHERE plugin_id = @pluginId";
        cmd.Parameters.AddWithValue("@pluginId", pluginId);

        await cmd.ExecuteNonQueryAsync();
    }

    // ==================== 临时数据操作（进程生命周期） ====================

    public async Task<object?> GetTempDataAsync(string pluginId, string key)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM temp_data WHERE plugin_id = @pluginId AND key = @key";
        cmd.Parameters.AddWithValue("@pluginId", pluginId);
        cmd.Parameters.AddWithValue("@key", key);

        return await cmd.ExecuteScalarAsync();
    }

    public async Task SetTempDataAsync(string pluginId, string key, string value)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO temp_data (plugin_id, key, value)
            VALUES (@pluginId, @key, @value)
            ON CONFLICT(plugin_id, key) DO UPDATE SET value = @value";
        cmd.Parameters.AddWithValue("@pluginId", pluginId);
        cmd.Parameters.AddWithValue("@key", key);
        cmd.Parameters.AddWithValue("@value", value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task ClearAllTempDataAsync()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM temp_data";
        await cmd.ExecuteNonQueryAsync();
    }

    // ==================== 插件管理 ====================

    public async Task SavePluginAsync(PluginInfo plugin)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO plugins (id, name, version, description, author, icon, entry_path, module_code, scoped_css, is_enabled, installed_at)
            VALUES (@id, @name, @version, @description, @author, @icon, @entryPath, @moduleCode, @scopedCss, @isEnabled, @installedAt)";
        cmd.Parameters.AddWithValue("@id", plugin.Id);
        cmd.Parameters.AddWithValue("@name", plugin.Name);
        cmd.Parameters.AddWithValue("@version", plugin.Version);
        cmd.Parameters.AddWithValue("@description", plugin.Description);
        cmd.Parameters.AddWithValue("@author", plugin.Author);
        cmd.Parameters.AddWithValue("@icon", plugin.Icon);
        cmd.Parameters.AddWithValue("@entryPath", plugin.EntryPath);
        cmd.Parameters.AddWithValue("@moduleCode", plugin.ModuleCode);
        cmd.Parameters.AddWithValue("@scopedCss", plugin.ScopedCss);
        cmd.Parameters.AddWithValue("@isEnabled", plugin.IsEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("@installedAt", plugin.InstalledAt.ToString("o"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeletePluginAsync(string pluginId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM plugins WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", pluginId);
        await cmd.ExecuteNonQueryAsync();

        // 同时清理插件数据
        await ClearPluginDataAsync(pluginId);
    }

    public async Task<List<PluginInfo>> GetAllPluginsAsync()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM plugins ORDER BY installed_at DESC";

        var plugins = new List<PluginInfo>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            plugins.Add(new PluginInfo
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Version = reader.GetString(2),
                Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Author = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Icon = reader.IsDBNull(5) ? "" : reader.GetString(5),
                EntryPath = reader.IsDBNull(6) ? "" : reader.GetString(6),
                ModuleCode = reader.IsDBNull(7) ? "" : reader.GetString(7),
                ScopedCss = reader.IsDBNull(8) ? "" : reader.GetString(8),
                IsEnabled = reader.IsDBNull(9) || reader.GetInt32(9) == 1,
                InstalledAt = DateTime.TryParse(reader.GetString(10), out var dt) ? dt : DateTime.UtcNow
            });
        }
        return plugins;
    }

    // ==================== 方案管理 ====================

    public async Task SaveSchemeAsync(Scheme scheme)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO schemes (id, name, target_device_id, layout, plugins, version, created_at, updated_at)
            VALUES (@id, @name, @targetDeviceId, @layout, @plugins, @version, @createdAt, @updatedAt)";
        cmd.Parameters.AddWithValue("@id", scheme.Id);
        cmd.Parameters.AddWithValue("@name", scheme.Name);
        cmd.Parameters.AddWithValue("@targetDeviceId", scheme.TargetDeviceId);
        cmd.Parameters.AddWithValue("@layout", System.Text.Json.JsonSerializer.Serialize(scheme.Layout));
        cmd.Parameters.AddWithValue("@plugins", System.Text.Json.JsonSerializer.Serialize(scheme.Plugins));
        cmd.Parameters.AddWithValue("@version", scheme.Version);
        cmd.Parameters.AddWithValue("@createdAt", scheme.CreatedAt.ToString("o"));
        cmd.Parameters.AddWithValue("@updatedAt", scheme.UpdatedAt.ToString("o"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Scheme?> GetSchemeForDeviceAsync(string deviceId)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM schemes WHERE target_device_id = @deviceId ORDER BY updated_at DESC LIMIT 1";
        cmd.Parameters.AddWithValue("@deviceId", deviceId);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Scheme
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                TargetDeviceId = reader.GetString(2),
                Layout = System.Text.Json.JsonSerializer.Deserialize<SchemeLayout>(reader.GetString(3)) ?? new(),
                Plugins = System.Text.Json.JsonSerializer.Deserialize<List<SchemePluginInstance>>(reader.GetString(4)) ?? [],
                Version = reader.GetInt32(5)
            };
        }
        return null;
    }

    // ==================== 日志操作 ====================

    public async Task InsertLogAsync(LogEntry entry)
    {
        if (_connection == null) return;

        var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO logs (level, message, source, stack_trace, device_id, plugin_id, timestamp)
            VALUES (@level, @message, @source, @stackTrace, @deviceId, @pluginId, @timestamp)";
        cmd.Parameters.AddWithValue("@level", entry.Level);
        cmd.Parameters.AddWithValue("@message", entry.Message);
        cmd.Parameters.AddWithValue("@source", entry.Source);
        cmd.Parameters.AddWithValue("@stackTrace", entry.StackTrace ?? "");
        cmd.Parameters.AddWithValue("@deviceId", entry.DeviceId ?? "");
        cmd.Parameters.AddWithValue("@pluginId", entry.PluginId ?? "");
        cmd.Parameters.AddWithValue("@timestamp", entry.Timestamp.ToString("o"));

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<LogQueryResult> QueryLogsAsync(LogQueryFilter filter)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var where = new List<string>();
        var cmd = _connection.CreateCommand();

        if (!string.IsNullOrEmpty(filter.Level))
        {
            where.Add("level = @level");
            cmd.Parameters.AddWithValue("@level", filter.Level);
        }
        if (!string.IsNullOrEmpty(filter.Source))
        {
            where.Add("source LIKE @source");
            cmd.Parameters.AddWithValue("@source", $"%{filter.Source}%");
        }
        if (!string.IsNullOrEmpty(filter.SearchText))
        {
            where.Add("message LIKE @search");
            cmd.Parameters.AddWithValue("@search", $"%{filter.SearchText}%");
        }
        if (!string.IsNullOrEmpty(filter.DeviceId))
        {
            where.Add("device_id = @deviceId");
            cmd.Parameters.AddWithValue("@deviceId", filter.DeviceId);
        }
        if (!string.IsNullOrEmpty(filter.PluginId))
        {
            where.Add("plugin_id = @pluginId");
            cmd.Parameters.AddWithValue("@pluginId", filter.PluginId);
        }
        if (filter.StartTime.HasValue)
        {
            where.Add("timestamp >= @startTime");
            cmd.Parameters.AddWithValue("@startTime", filter.StartTime.Value.ToString("o"));
        }
        if (filter.EndTime.HasValue)
        {
            where.Add("timestamp <= @endTime");
            cmd.Parameters.AddWithValue("@endTime", filter.EndTime.Value.ToString("o"));
        }

        var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

        // 获取总数
        cmd.CommandText = $"SELECT COUNT(*) FROM logs {whereClause}";
        var total = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        // 获取数据
        cmd.Parameters.AddWithValue("@offset", filter.Offset);
        cmd.Parameters.AddWithValue("@limit", filter.Limit);
        cmd.CommandText = $"SELECT * FROM logs {whereClause} ORDER BY timestamp DESC LIMIT @limit OFFSET @offset";

        var entries = new List<LogEntry>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(new LogEntry
            {
                Id = reader.GetInt64(0),
                Level = reader.GetString(1),
                Message = reader.GetString(2),
                Source = reader.IsDBNull(3) ? "" : reader.GetString(3),
                StackTrace = reader.IsDBNull(4) ? null : reader.GetString(4),
                DeviceId = reader.IsDBNull(5) ? null : reader.GetString(5),
                PluginId = reader.IsDBNull(6) ? null : reader.GetString(6),
                Timestamp = DateTime.TryParse(reader.GetString(7), out var dt) ? dt : DateTime.UtcNow
            });
        }

        return new LogQueryResult
        {
            Entries = entries,
            Total = total,
            Offset = filter.Offset,
            Limit = filter.Limit
        };
    }

    public async Task PruneLogsAsync(int maxEntries, int pruneBatch)
    {
        if (_connection == null) return;

        var cmd = _connection.CreateCommand();
        cmd.CommandText = $"DELETE FROM logs WHERE id IN (SELECT id FROM logs ORDER BY timestamp DESC LIMIT {maxEntries} OFFSET {maxEntries - pruneBatch})";
        await cmd.ExecuteNonQueryAsync();
    }
}
