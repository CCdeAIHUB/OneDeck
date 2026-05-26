using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace OneDeck.Desktop;

/// <summary>
/// 主窗口 - 使用 WebView2 承载 Vue 前端
/// 自绘标题栏：前端 Vue 自行绘制，窗口设为无边框
/// </summary>
public class MainForm : Form
{
    private WebView2 webView;
    private readonly int _httpPort;

    public MainForm(int httpPort)
    {
        _httpPort = httpPort;
        InitializeForm();
        InitializeWebView();
    }

    private void InitializeForm()
    {
        // 窗口基本设置
        Text = "OneDeck";
        Size = new Size(1280, 800);
        MinimumSize = new Size(960, 600);
        StartPosition = FormStartPosition.CenterScreen;

        // 无边框 - 标题栏由前端 Vue 自行绘制
        FormBorderStyle = FormBorderStyle.None;
        DoubleBuffered = true;

        // 窗口拖拽支持（前端标题栏区域触发拖拽通过 WebView2 消息传递）
    }

    private async void InitializeWebView()
    {
        // 创建 WebView2 控件
        webView = new WebView2
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(webView);

        try
        {
            // 初始化 WebView2 运行时
            await webView.EnsureCoreWebView2Async(null);

            // 注册 WebView2 消息处理（前端可通过 postMessage 与 C# 通讯）
            webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            // 导航到前端页面
            var url = $"http://localhost:{_httpPort}";
            webView.CoreWebView2.Navigate(url);

            // 开发者工具（开发模式下可用）
            webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"无法初始化 WebView2。请确保已安装 Microsoft Edge WebView2 Runtime。\n\n错误：{ex.Message}",
                "OneDeck 启动失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            Application.Exit();
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
                break;
            case "window:close":
                Close();
                break;
        }
    }

    /// <summary>
    /// 允许无边框窗口拖拽（点击非 WebView 区域时）
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        // 处理 WM_NCHITTEST 实现窗口拖拽
        const int WM_NCHITTEST = 0x84;
        const int HTCLIENT = 1;
        const int HTCAPTION = 2;

        base.WndProc(ref m);

        if (m.Msg == WM_NCHITTEST)
        {
            // 如果鼠标在客户区，允许通过标题栏拖拽
            // 实际拖拽由前端标题栏触发 postMessage 处理
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        // 通知前端窗口大小变化
        try
        {
            webView?.CoreWebView2?.PostWebMessageAsJson(
                $"{{\"type\":\"resize\",\"width\":{ClientSize.Width},\"height\":{ClientSize.Height},\"state\":\"{WindowState}\"}}"
            );
        }
        catch { }
    }
}
