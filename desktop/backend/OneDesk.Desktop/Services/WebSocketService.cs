using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OneDesk.Desktop.Models;

namespace OneDesk.Desktop.Services;

/// <summary>
/// WebSocket 服务器服务
/// 管理所有移动端设备的 WebSocket 连接
/// 处理双向通讯：接收移动端消息 + 向移动端推送方案/状态/指令
/// </summary>
public class WebSocketService
{
    private readonly LogService _logService;
    private readonly PluginService _pluginService;
    private readonly SchemeService _schemeService;
    private readonly JsApiService _jsApiService;

    private readonly Dictionary<string, DeviceConnection> _connections = new();
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private Task? _heartbeatTask;

    public event EventHandler<DeviceConnectedEventArgs>? DeviceConnected;
    public event EventHandler<DeviceDisconnectedEventArgs>? DeviceDisconnected;
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public WebSocketService(
        LogService logService,
        PluginService pluginService,
        SchemeService schemeService,
        JsApiService jsApiService)
    {
        _logService = logService;
        _pluginService = pluginService;
        _schemeService = schemeService;
        _jsApiService = jsApiService;
    }

    public void Start(int port)
    {
        _cts = new CancellationTokenSource();

        // TODO: 启动 WebSocketSharp 或 System.Net.WebSockets 服务器
        _logService.Info("WebSocketService", $"WebSocket server listening on port {port}");

        // 启动心跳检测任务
        _heartbeatTask = Task.Run(() => HeartbeatLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        lock (_lock)
        {
            foreach (var conn in _connections.Values)
            {
                conn.Dispose();
            }
            _connections.Clear();
        }
    }

    /// <summary>
    /// 向指定设备推送方案
    /// </summary>
    public async Task PushSchemeToDeviceAsync(string deviceId)
    {
        var scheme = await _schemeService.GetSchemeForDeviceAsync(deviceId);
        if (scheme == null)
        {
            _logService.Warn("WebSocketService", $"No scheme assigned for device {deviceId}");
            return;
        }

        // 下发方案结构
        await SendToDeviceAsync(deviceId, new WsMessage
        {
            Type = "scheme_push",
            DeviceId = deviceId,
            Payload = new Dictionary<string, object>
            {
                ["schemeId"] = scheme.Id,
                ["schemeName"] = scheme.Name,
                ["layout"] = scheme.Layout,
                ["plugins"] = scheme.Plugins,
                ["version"] = scheme.Version
            }
        });

        // 下发方案中每个插件的完整 JS 模块和样式
        foreach (var pluginInstance in scheme.Plugins)
        {
            var plugin = await _pluginService.GetPluginAsync(pluginInstance.PluginId);
            if (plugin == null) continue;

            // 下发 JS 模块
            await SendToDeviceAsync(deviceId, new WsMessage
            {
                Type = "module_push",
                DeviceId = deviceId,
                PluginId = pluginInstance.PluginId,
                Payload = new Dictionary<string, object>
                {
                    ["pluginId"] = pluginInstance.PluginId,
                    ["instanceId"] = pluginInstance.InstanceId,
                    ["moduleCode"] = plugin.ModuleCode,
                    ["moduleId"] = plugin.Id,
                    ["version"] = plugin.Version
                }
            });

            // 下发 Scoped CSS
            if (!string.IsNullOrEmpty(plugin.ScopedCss))
            {
                await SendToDeviceAsync(deviceId, new WsMessage
                {
                    Type = "style_push",
                    DeviceId = deviceId,
                    PluginId = pluginInstance.PluginId,
                    Payload = new Dictionary<string, object>
                    {
                        ["pluginId"] = pluginInstance.PluginId,
                        ["css"] = plugin.ScopedCss,
                        ["styleId"] = $"style-{plugin.Id}"
                    }
                });
            }
        }

        _logService.Info("WebSocketService", $"Scheme '{scheme.Name}' pushed to device {deviceId}");
    }

    /// <summary>
    /// 同步插件状态到移动端
    /// </summary>
    public async Task SyncStateToDeviceAsync(string deviceId, string pluginId, string instanceId, Dictionary<string, object> state)
    {
        await SendToDeviceAsync(deviceId, new WsMessage
        {
            Type = "state_sync",
            DeviceId = deviceId,
            PluginId = pluginId,
            Payload = new Dictionary<string, object>
            {
                ["pluginId"] = pluginId,
                ["instanceId"] = instanceId,
                ["state"] = state,
                ["source"] = "desktop"
            }
        });
    }

    /// <summary>
    /// 发送指令到移动端
    /// </summary>
    public async Task SendCommandAsync(string deviceId, string command, Dictionary<string, object>? data = null)
    {
        await SendToDeviceAsync(deviceId, new WsMessage
        {
            Type = "command",
            DeviceId = deviceId,
            Payload = new Dictionary<string, object>
            {
                ["command"] = command,
                ["data"] = data ?? new Dictionary<string, object>()
            }
        });
    }

    /// <summary>
    /// 处理从移动端收到的消息
    /// </summary>
    public async Task HandleMessageAsync(string deviceId, WsMessage message)
    {
        switch (message.Type)
        {
            case "device_register":
                await HandleDeviceRegisterAsync(deviceId, message);
                break;

            case "device_heartbeat":
                HandleHeartbeat(deviceId, message);
                break;

            case "jsapi_call":
                await HandleJsApiCallAsync(deviceId, message);
                break;

            case "event_report":
                await HandleEventReportAsync(deviceId, message);
                break;

            case "log_transfer":
                HandleLogTransfer(deviceId, message);
                break;

            case "log_batch_transfer":
                HandleLogBatchTransfer(deviceId, message);
                break;

            case "permission_request":
                await HandlePermissionRequestAsync(deviceId, message);
                break;

            default:
                _logService.Warn("WebSocketService", $"Unknown message type: {message.Type}");
                break;
        }

        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(deviceId, message));
    }

    // ==================== 内部处理方法 ====================

    private async Task HandleDeviceRegisterAsync(string deviceId, WsMessage message)
    {
        var payload = message.Payload;
        var device = new ConnectedDevice
        {
            DeviceId = deviceId,
            DeviceName = payload.GetValueOrDefault("deviceName", "").ToString() ?? "",
            Platform = payload.GetValueOrDefault("platform", "").ToString() ?? "",
            OsVersion = payload.GetValueOrDefault("osVersion", "").ToString() ?? "",
            AppId = payload.GetValueOrDefault("appId", "").ToString() ?? "",
            ConnectedAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow
        };

        lock (_lock)
        {
            _connections[deviceId] = new DeviceConnection(deviceId, device);
        }

        _logService.Info("WebSocketService", $"Device registered: {device.DeviceName} ({device.Platform})");

        DeviceConnected?.Invoke(this, new DeviceConnectedEventArgs(device));

        // 自动推送已分配的方案
        await PushSchemeToDeviceAsync(deviceId);
    }

    private void HandleHeartbeat(string deviceId, WsMessage message)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(deviceId, out var conn))
            {
                conn.Device.LastHeartbeat = DateTime.UtcNow;
                conn.Device.IsOnline = true;
            }
        }
    }

    private async Task HandleJsApiCallAsync(string deviceId, WsMessage message)
    {
        var apiName = message.Payload.GetValueOrDefault("apiName", "").ToString() ?? "";
        var args = message.Payload.GetValueOrDefault("args", new Dictionary<string, object>()) as Dictionary<string, object> ?? new();
        var callId = message.Payload.GetValueOrDefault("callId", "").ToString() ?? "";

        var result = await _jsApiService.ExecuteApiAsync(apiName, args, deviceId, message.PluginId);

        await SendToDeviceAsync(deviceId, new WsMessage
        {
            Type = "jsapi_response",
            DeviceId = deviceId,
            PluginId = message.PluginId,
            Payload = new Dictionary<string, object>
            {
                ["callId"] = callId,
                ["success"] = result.IsSuccess,
                ["result"] = result.Data ?? new(),
                ["error"] = result.ErrorMessage ?? ""
            }
        });
    }

    private async Task HandleEventReportAsync(string deviceId, WsMessage message)
    {
        // 桌面端驱动模式：移动端事件由桌面端插件处理
        var pluginId = message.PluginId;
        var eventType = message.Payload.GetValueOrDefault("eventType", "").ToString();
        var eventData = message.Payload.GetValueOrDefault("eventData", new Dictionary<string, object>());

        _logService.Debug("WebSocketService", $"Event from {deviceId}/{pluginId}: {eventType}");

        // 通知桌面端前端处理事件
        MessageReceived?.Invoke(this, new MessageReceivedEventArgs(deviceId, message));
    }

    private void HandleLogTransfer(string deviceId, WsMessage message)
    {
        var level = message.Payload.GetValueOrDefault("level", "info").ToString() ?? "info";
        var logMessage = message.Payload.GetValueOrDefault("message", "").ToString() ?? "";
        var source = message.Payload.GetValueOrDefault("source", "").ToString() ?? "";
        var stackTrace = message.Payload.GetValueOrDefault("stackTrace", "")?.ToString();

        _logService.Log(level, source, logMessage, stackTrace, deviceId, message.PluginId);
    }

    private void HandleLogBatchTransfer(string deviceId, WsMessage message)
    {
        // 断连补传的批量日志
        var logs = message.Payload.GetValueOrDefault("logs", new List<object>()) as List<object>;
        if (logs == null) return;

        foreach (var logObj in logs)
        {
            if (logObj is Dictionary<string, object> log)
            {
                var level = log.GetValueOrDefault("level", "info").ToString() ?? "info";
                var logMessage = log.GetValueOrDefault("message", "").ToString() ?? "";
                var source = log.GetValueOrDefault("source", "").ToString() ?? "";
                var stackTrace = log.GetValueOrDefault("stackTrace", "")?.ToString();
                _logService.Log(level, source, logMessage, stackTrace, deviceId, message.PluginId);
            }
        }

        _logService.Info("WebSocketService", $"Batch log received from {deviceId}: {logs.Count} entries");
    }

    private async Task HandlePermissionRequestAsync(string deviceId, WsMessage message)
    {
        var permission = message.Payload.GetValueOrDefault("permission", "").ToString() ?? "";
        var reason = message.Payload.GetValueOrDefault("reason", "").ToString() ?? "";

        _logService.Info("WebSocketService", $"Permission request from {deviceId}: {permission} - {reason}");

        // 桌面端记录权限请求日志，实际权限授予由移动端系统弹窗处理
        // 移动端会将结果通过 jsapi_response 返回给调用方
    }

    private async Task SendToDeviceAsync(string deviceId, WsMessage message)
    {
        lock (_lock)
        {
            if (_connections.TryGetValue(deviceId, out var conn) && conn.IsOnline)
            {
                // TODO: 序列化消息并通过 WebSocket 发送
                // conn.WebSocket.Send(JsonSerializer.Serialize(message));
            }
        }
        await Task.CompletedTask;
    }

    private async Task HeartbeatLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(10000, ct); // 每10秒检测一次

            var now = DateTime.UtcNow;
            lock (_lock)
            {
                foreach (var conn in _connections.Values)
                {
                    if ((now - conn.Device.LastHeartbeat).TotalSeconds > 30)
                    {
                        if (conn.Device.IsOnline)
                        {
                            conn.Device.IsOnline = false;
                            _logService.Warn("WebSocketService", $"Device {conn.Device.DeviceName} heartbeat timeout");
                            DeviceDisconnected?.Invoke(this, new DeviceDisconnectedEventArgs(conn.Device.DeviceId, "heartbeat_timeout"));
                        }
                    }
                }
            }
        }
    }
}

// ==================== 事件参数 ====================

public class DeviceConnectedEventArgs : EventArgs
{
    public ConnectedDevice Device { get; }
    public DeviceConnectedEventArgs(ConnectedDevice device) => Device = device;
}

public class DeviceDisconnectedEventArgs : EventArgs
{
    public string DeviceId { get; }
    public string Reason { get; }
    public DeviceDisconnectedEventArgs(string deviceId, string reason)
    {
        DeviceId = deviceId;
        Reason = reason;
    }
}

public class MessageReceivedEventArgs : EventArgs
{
    public string DeviceId { get; }
    public WsMessage Message { get; }
    public MessageReceivedEventArgs(string deviceId, WsMessage message)
    {
        DeviceId = deviceId;
        Message = message;
    }
}

// ==================== 设备连接封装 ====================

public class DeviceConnection : IDisposable
{
    public string DeviceId { get; }
    public ConnectedDevice Device { get; }
    public bool IsOnline => Device.IsOnline;

    // TODO: WebSocket 实例引用
    // public WebSocket WebSocket { get; set; }

    public DeviceConnection(string deviceId, ConnectedDevice device)
    {
        DeviceId = deviceId;
        Device = device;
    }

    public void Dispose()
    {
        Device.IsOnline = false;
    }
}

// ==================== 辅助扩展 ====================

public static class DictionaryExtensions
{
    public static object? GetValueOrDefault(this Dictionary<string, object> dict, string key, object? defaultValue = null)
    {
        return dict.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
