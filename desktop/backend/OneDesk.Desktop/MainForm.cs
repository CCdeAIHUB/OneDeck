using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using OneDesk.Desktop.Services;

namespace OneDesk.Desktop;

/// <summary>
/// 主窗口 - 使用 WebView2 承载 Vue 前端
/// 自绘标题栏：前端 Vue 自行绘制，窗口设为无边框
/// 前端资源通过 WebView2 SetVirtualHostNameToFolderMapping 以文件方式加载，无需 HTTP 服务器
/// </summary>
public class MainForm : Form
{
    private WebView2? webView;
    private readonly int _wsPort;
    private StorageService? _storageService;
    private LogService? _logService;

    // Win32 API 用于无边框窗口拖拽
    [DllImport("user32.dll")]
    private static extern int ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 2;

    public MainForm(int wsPort)
    {
        _wsPort = wsPort;
        InitializeForm();
        InitializeBackend();
    }

    private void InitializeForm()
    {
        // 窗口基本设置
        Text = "OneDesk";
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

            // 2. 创建 WebView2 控件
            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(webView);

            // 3. 初始化 WebView2 运行时
            await webView.EnsureCoreWebView2Async(null);

            // 注册消息处理（前端通过 postMessage 与 C# 通讯）
            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            // 4. 确定前端文件路径
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

            // 5. 使用 SetVirtualHostNameToFolderMapping 将前端文件映射为虚拟主机
            // 前端资源以文件方式加载，无需 HTTP 服务器
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "onedesk.local",
                wwwrootPath,
                Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
            );

            // 6. 导航到虚拟主机上的 index.html
            webView.CoreWebView2.Navigate("https://onedesk.local/index.html");

            // 开发者工具
            webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

            _logService.Info("MainForm", $"WebView2 navigated to https://onedesk.local (root: {wwwrootPath})");
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
                // 同步窗口状态到前端
                PostWindowState();
                break;
            case "window:close":
                Close();
                break;
            case "window:getState":
                PostWindowState();
                break;
            case "window:drag":
                // 前端标题栏拖拽请求：释放鼠标捕获并模拟标题栏拖拽
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                break;
        }
    }

    /// <summary>
    /// 将窗口最大化状态发送到前端
    /// </summary>
    private void PostWindowState()
    {
        if (webView?.CoreWebView2 != null)
        {
            var isMax = WindowState == FormWindowState.Maximized;
            webView.CoreWebView2.PostWebMessageAsJson(
                $"{{\"type\":\"window:state\",\"maximized\":{(isMax ? "true" : "false")}}}"
            );
        }
    }

    /// <summary>
    /// 窗口状态变化时同步到前端
    /// </summary>
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PostWindowState();
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
