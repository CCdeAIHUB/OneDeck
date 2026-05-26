using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using OneDeck.Desktop.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;

namespace OneDeck.Desktop;

/// <summary>
/// 主窗口 - 使用 WebView2 承载 Vue 前端
/// 自绘标题栏：前端 Vue 自行绘制，窗口设为无边框
/// </summary>
public class MainForm : Form
{
    private WebView2? webView;
    private readonly int _httpPort;
    private readonly int _wsPort;
    private StorageService? _storageService;
    private LogService? _logService;

    public MainForm(int httpPort, int wsPort)
    {
        _httpPort = httpPort;
        _wsPort = wsPort;
        InitializeForm();
        InitializeBackend();
    }

    private void InitializeForm()
    {
        // 窗口基本设置
        Text = "OneDeck";
        Size = new Size(1280, 800);
        MinimumSize = new Size(960, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(3, 7, 18); // gray-950

        // 无边框 - 标题栏由前端 Vue 自行绘制
        FormBorderStyle = FormBorderStyle.None;
        DoubleBuffered = true;
    }

    private async void InitializeBackend()
    {
        try
        {
            // 1. 初始化后端服务
            _storageService = new StorageService();
            await _storageService.InitializeAsync();
            _logService = new LogService(_storageService);

            _logService.Info("MainForm", "Backend services initialized");

            // 2. 启动 Kestrel HTTP 服务器
            await StartHttpServerAsync(_httpPort);

            // 3. 创建 WebView2 控件
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(webView);

            // 4. 初始化 WebView2 运行时
            await webView.EnsureCoreWebView2Async(null);

            // 注册消息处理（前端通过 postMessage 与 C# 通讯）
            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            // 5. 导航到前端页面
            var url = $"http://localhost:{_httpPort}";
            webView.CoreWebView2.Navigate(url);

            // 开发者工具
            webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

            _logService.Info("MainForm", $"WebView2 navigated to {url}");
        }
        catch (Exception ex)
        {
            // 如果 WebView2 初始化失败，显示错误信息
            var errorLabel = new Label
            {
                Text = $"无法初始化 WebView2 运行时。\n\n" +
                       $"请确保已安装 Microsoft Edge WebView2 Runtime。\n" +
                       $"下载地址：https://developer.microsoft.com/microsoft-edge/webview2/\n\n" +
                       $"错误详情：{ex.Message}",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(3, 7, 18),
                Font = new Font("Segoe UI", 12),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(40)
            };
            Controls.Add(errorLabel);
            errorLabel.BringToFront();
        }
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
        if (!System.IO.Directory.Exists(wwwrootPath) || !System.IO.Directory.EnumerateFiles(wwwrootPath, "*.html").Any())
        {
            // 尝试开发目录
            var devPath = System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "frontend", "dist");
            if (System.IO.Directory.Exists(devPath))
            {
                wwwrootPath = System.IO.Path.GetFullPath(devPath);
            }
            else
            {
                System.IO.Directory.CreateDirectory(wwwrootPath);
            }
        }

        // 提供静态文件
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = new PhysicalFileProvider(wwwrootPath)
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(wwwrootPath)
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

        await app.StartAsync();
        _logService?.Info("MainForm", $"HTTP server started on http://localhost:{port} (root: {wwwrootPath})");
    }

    /// <summary>
    /// 处理来自前端的消息
    /// 前端通过 window.chrome.webview.postMessage() 发送消息
    /// </summary>
    private void OnWebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString();
        switch (message)
        {
            case "window:minimize":
                WindowState = FormWindowState.Minimized;
                break;
            case "window:maximize":
                if (WindowState == FormWindowState.Maximized)
                    WindowState = FormWindowState.Normal;
                else
                    WindowState = FormWindowState.Maximized;
                break;
            case "window:close":
                Close();
                break;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // 清理后端服务
        _storageService?.CloseAsync().Wait();
        base.OnFormClosed(e);
    }

    /// <summary>
    /// 允许无边框窗口通过鼠标拖拽移动
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        const int HTCAPTION = 2;

        base.WndProc(ref m);

        if (m.Msg == WM_NCHITTEST && (int)m.Result == 1)
        {
            // 将客户区点击转换为标题栏拖拽
            // 仅在顶部 40px（标题栏高度）区域生效
            var pos = PointToClient(new Point(m.LParam.ToInt32()));
            if (pos.Y < 40)
            {
                m.Result = (IntPtr)HTCAPTION;
            }
        }
    }
}
