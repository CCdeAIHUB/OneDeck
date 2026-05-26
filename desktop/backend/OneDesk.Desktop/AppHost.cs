using OneDeck.Desktop.Models;
using OneDeck.Desktop.Services;

namespace OneDeck.Desktop;

/// <summary>
/// 桌面端应用主入口
/// 职责：初始化所有服务、启动 WebSocket 服务器、加载前端
/// </summary>
public class AppHost
{
    private readonly WebSocketService _webSocketService;
    private readonly JsApiService _jsApiService;
    private readonly PluginService _pluginService;
    private readonly LogService _logService;
    private readonly StorageService _storageService;
    private readonly SchemeService _schemeService;

    public AppHost()
    {
        _storageService = new StorageService();
        _logService = new LogService(_storageService);
        _pluginService = new PluginService(_storageService, _logService);
        _jsApiService = new JsApiService(_logService);
        _schemeService = new SchemeService(_storageService, _logService);
        _webSocketService = new WebSocketService(
            _logService,
            _pluginService,
            _schemeService,
            _jsApiService
        );
    }

    public async Task StartAsync(int wsPort = 9720, int httpPort = 9721)
    {
        // 初始化数据库
        await _storageService.InitializeAsync();

        // 加载已安装的插件
        await _pluginService.LoadInstalledPluginsAsync();

        // 启动 WebSocket 服务器
        _webSocketService.Start(wsPort);

        // 启动 HTTP 服务器（提供前端静态文件）
        StartHttpServer(httpPort);

        _logService.Info("AppHost", $"OneDeck Desktop started. WS: ws://0.0.0.0:{wsPort}, HTTP: http://localhost:{httpPort}");

        // 等待关闭信号
        var tcs = new TaskCompletionSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            tcs.TrySetResult();
        };
        await tcs.Task;

        await StopAsync();
    }

    public async Task StopAsync()
    {
        _webSocketService.Stop();
        await _storageService.CloseAsync();
        _logService.Info("AppHost", "OneDeck Desktop stopped.");
    }

    private void StartHttpServer(int port)
    {
        // TODO: 使用 ASP.NET Core Kestrel 或自定义 HTTP 服务器
        // 提供桌面端前端 Vue 应用的静态文件
        _logService.Info("AppHost", $"HTTP server started on port {port}");
    }
}
