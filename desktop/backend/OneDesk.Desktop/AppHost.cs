using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using OneDeck.Desktop.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;

namespace OneDeck.Desktop;

/// <summary>
/// 桌面端应用主入口
/// 职责：初始化所有服务、启动 WebSocket 服务器、启动 HTTP 服务器、打开 WebView2 窗口
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

        // 启动 Kestrel HTTP 服务器（提供前端静态文件）
        var httpServerTask = StartHttpServerAsync(httpPort);

        _logService.Info("AppHost", $"OneDeck Desktop started. WS: ws://0.0.0.0:{wsPort}, HTTP: http://localhost:{httpPort}");

        // 启动 WebView2 窗口（在 UI 线程）
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(httpPort));

        // 窗口关闭后清理
        await StopAsync();
    }

    public async Task StopAsync()
    {
        _webSocketService.Stop();
        await _storageService.CloseAsync();
        _logService.Info("AppHost", "OneDeck Desktop stopped.");
    }

    /// <summary>
    /// 启动 Kestrel HTTP 服务器，提供前端 Vue 应用的静态文件
    /// </summary>
    private async Task StartHttpServerAsync(int port)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

        var app = builder.Build();

        // 确定前端文件路径
        var wwwrootPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot");
        if (!System.IO.Directory.Exists(wwwrootPath))
        {
            wwwrootPath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "frontend", "dist");
            if (!System.IO.Directory.Exists(wwwrootPath))
            {
                wwwrootPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot");
                System.IO.Directory.CreateDirectory(wwwrootPath);
            }
        }

        // 提供静态文件
        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(wwwrootPath),
            RequestPath = ""
        });

        // SPA 路由回退
        app.MapFallback(async (HttpContext ctx) =>
        {
            var indexPath = System.IO.Path.Combine(wwwrootPath, "index.html");
            if (System.IO.File.Exists(indexPath))
            {
                ctx.Response.ContentType = "text/html";
                await ctx.Response.SendFileAsync(indexPath);
            }
            else
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.WriteAsync("Frontend not built. Run: cd desktop/frontend && npm run build");
            }
        });

        // 启动 HTTP 服务器（非阻塞）
        await app.StartAsync();

        _logService.Info("AppHost", $"HTTP server serving from: {wwwrootPath}");
    }
}
