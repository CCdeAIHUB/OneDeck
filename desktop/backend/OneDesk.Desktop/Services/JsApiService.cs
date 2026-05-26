namespace OneDeck.Desktop.Services;

/// <summary>
/// JSAPI 服务
/// 处理来自移动端/桌面前端的 JSAPI 调用
/// 所有设备能力均通过此服务暴露，插件只能消费返回数据
/// </summary>
public class JsApiService
{
    private readonly LogService _logService;

    private readonly Dictionary<string, Func<Dictionary<string, object>, string, string?, Task<JsApiResult>>> _apiHandlers = new();

    public JsApiService(LogService logService)
    {
        _logService = logService;
        RegisterBuiltinApis();
    }

    /// <summary>
    /// 注册自定义 JSAPI
    /// </summary>
    public void RegisterApi(string apiName, Func<Dictionary<string, object>, string, string?, Task<JsApiResult>> handler)
    {
        _apiHandlers[apiName] = handler;
    }

    /// <summary>
    /// 执行 JSAPI 调用
    /// </summary>
    public async Task<JsApiResult> ExecuteApiAsync(string apiName, Dictionary<string, object> args, string deviceId, string? pluginId)
    {
        if (!_apiHandlers.TryGetValue(apiName, out var handler))
        {
            _logService.Warn("JsApiService", $"Unknown JSAPI: {apiName}");
            return JsApiResult.Error($"Unknown API: {apiName}");
        }

        try
        {
            var result = await handler(args, deviceId, pluginId);
            _logService.Debug("JsApiService", $"JSAPI {apiName} called by {pluginId ?? "system"} on {deviceId}: {(result.Success ? "OK" : "FAIL")}");
            return result;
        }
        catch (Exception ex)
        {
            _logService.Error("JsApiService", $"JSAPI {apiName} error: {ex.Message}", ex.StackTrace);
            return JsApiResult.Error(ex.Message);
        }
    }

    private void RegisterBuiltinApis()
    {
        // 文件操作
        RegisterApi("file.read", async (args, deviceId, pluginId) =>
        {
            var path = args.GetValueOrDefault("path", "")?.ToString() ?? "";
            // TODO: 实现文件读取，限制在插件沙箱目录内
            await Task.CompletedTask;
            return JsApiResult.Success(new { content = "" });
        });

        RegisterApi("file.write", async (args, deviceId, pluginId) =>
        {
            var path = args.GetValueOrDefault("path", "")?.ToString() ?? "";
            var content = args.GetValueOrDefault("content", "")?.ToString() ?? "";
            // TODO: 实现文件写入，限制在插件沙箱目录内
            await Task.CompletedTask;
            return JsApiResult.Success();
        });

        RegisterApi("file.delete", async (args, deviceId, pluginId) =>
        {
            var path = args.GetValueOrDefault("path", "")?.ToString() ?? "";
            // TODO: 实现文件删除
            await Task.CompletedTask;
            return JsApiResult.Success();
        });

        RegisterApi("file.exists", async (args, deviceId, pluginId) =>
        {
            var path = args.GetValueOrDefault("path", "")?.ToString() ?? "";
            // TODO: 实现文件存在检查
            await Task.CompletedTask;
            return JsApiResult.Success(new { exists = false });
        });

        RegisterApi("file.list", async (args, deviceId, pluginId) =>
        {
            var dir = args.GetValueOrDefault("dir", "")?.ToString() ?? "";
            // TODO: 实现目录列表
            await Task.CompletedTask;
            return JsApiResult.Success(new { files = Array.Empty<string>() });
        });

        // 设备信息
        RegisterApi("device.getInfo", async (args, deviceId, pluginId) =>
        {
            await Task.CompletedTask;
            return JsApiResult.Success(new
            {
                platform = "desktop",
                osVersion = Environment.OSVersion.ToString(),
                model = "OneDeck Desktop",
                screenResolution = new { width = 1920, height = 1080 }
            });
        });

        // 内存读取（仅桌面端，macOS 不可用）
        RegisterApi("memory.readProcessMemory", async (args, deviceId, pluginId) =>
        {
            if (OperatingSystem.IsMacOS())
            {
                return JsApiResult.Error("Platform not supported: macOS SIP prevents memory reading");
            }

            var pid = Convert.ToInt32(args.GetValueOrDefault("pid", 0));
            var offset = Convert.ToInt64(args.GetValueOrDefault("offset", 0));
            var size = Convert.ToInt32(args.GetValueOrDefault("size", 0));

            // TODO: 实现跨平台内存读取（Windows: ReadProcessMemory, Linux: /proc/pid/mem）
            await Task.CompletedTask;
            return JsApiResult.Success(new { data = Array.Empty<byte>() });
        });

        RegisterApi("memory.getProcessList", async (args, deviceId, pluginId) =>
        {
            // TODO: 实现进程列表获取
            await Task.CompletedTask;
            return JsApiResult.Success(new { processes = Array.Empty<object>() });
        });

        RegisterApi("memory.readWindowInfo", async (args, deviceId, pluginId) =>
        {
            // TODO: 实现窗口信息读取
            await Task.CompletedTask;
            return JsApiResult.Success(new
            {
                title = "",
                bounds = new { x = 0, y = 0, width = 0, height = 0 },
                isForeground = false
            });
        });

        // 剪贴板
        RegisterApi("clipboard.read", async (args, deviceId, pluginId) =>
        {
            // TODO: 读取剪贴板
            await Task.CompletedTask;
            return JsApiResult.Success(new { text = "" });
        });

        RegisterApi("clipboard.write", async (args, deviceId, pluginId) =>
        {
            var text = args.GetValueOrDefault("text", "")?.ToString() ?? "";
            // TODO: 写入剪贴板
            await Task.CompletedTask;
            return JsApiResult.Success();
        });

        // 网络
        RegisterApi("network.getStatus", async (args, deviceId, pluginId) =>
        {
            // TODO: 获取网络状态
            await Task.CompletedTask;
            return JsApiResult.Success(new { connected = true, type = "ethernet" });
        });

        // 通知
        RegisterApi("notification.show", async (args, deviceId, pluginId) =>
        {
            var title = args.GetValueOrDefault("title", "")?.ToString() ?? "";
            var body = args.GetValueOrDefault("body", "")?.ToString() ?? "";
            // TODO: 显示系统通知
            await Task.CompletedTask;
            return JsApiResult.Success();
        });

        // 存储
        RegisterApi("storage.get", async (args, deviceId, pluginId) =>
        {
            var key = args.GetValueOrDefault("key", "")?.ToString() ?? "";
            // TODO: 从 SQLite 读取插件数据（按 plugin_id 隔离）
            await Task.CompletedTask;
            return JsApiResult.Success(new { value = (object?)null });
        });

        RegisterApi("storage.set", async (args, deviceId, pluginId) =>
        {
            var key = args.GetValueOrDefault("key", "")?.ToString() ?? "";
            var value = args.GetValueOrDefault("value");
            // TODO: 写入 SQLite 插件数据（按 plugin_id 隔离）
            await Task.CompletedTask;
            return JsApiResult.Success();
        });

        RegisterApi("storage.remove", async (args, deviceId, pluginId) =>
        {
            var key = args.GetValueOrDefault("key", "")?.ToString() ?? "";
            // TODO: 删除 SQLite 插件数据
            await Task.CompletedTask;
            return JsApiResult.Success();
        });

        RegisterApi("storage.keys", async (args, deviceId, pluginId) =>
        {
            // TODO: 获取插件所有存储键
            await Task.CompletedTask;
            return JsApiResult.Success(new { keys = Array.Empty<string>() });
        });

        RegisterApi("storage.clear", async (args, deviceId, pluginId) =>
        {
            // TODO: 清空插件存储
            await Task.CompletedTask;
            return JsApiResult.Success();
        });
    }
}

/// <summary>
/// JSAPI 调用结果
/// </summary>
public class JsApiResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string? Error { get; init; }

    public static JsApiResult Success(object? data = null) => new() { Success = true, Data = data };
    public static JsApiResult Error(string error) => new() { Success = false, Error = error };
}
