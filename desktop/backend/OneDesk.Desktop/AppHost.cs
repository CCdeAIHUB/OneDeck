using System;
using System.Threading.Tasks;
using OneDesk.Desktop.Services;

namespace OneDesk.Desktop;

/// <summary>
/// 桌面端应用主入口（已迁移到 MainForm 中启动）
/// 此类保留作为服务容器，供后续 DI 容器化改造使用
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
        _jsApiService = new JsApiService(_logService, _storageService);
        _schemeService = new SchemeService(_storageService, _logService);
        _webSocketService = new WebSocketService(
            _logService,
            _pluginService,
            _schemeService,
            _jsApiService
        );
    }

    public StorageService StorageService => _storageService;
    public LogService LogService => _logService;
    public WebSocketService WebSocketService => _webSocketService;
    public PluginService PluginService => _pluginService;
    public JsApiService JsApiService => _jsApiService;
    public SchemeService SchemeService => _schemeService;

    /// <summary>
    /// 初始化后端服务（在 MainForm 中调用）
    /// </summary>
    public async Task InitializeAsync()
    {
        await _storageService.InitializeAsync();
        await _pluginService.LoadInstalledPluginsAsync();
        _webSocketService.Start(9720);
    }

    public async Task StopAsync()
    {
        _webSocketService.Stop();
        await _storageService.CloseAsync();
    }
}
