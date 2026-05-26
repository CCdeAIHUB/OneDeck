using System;
using System.Windows.Forms;
using OneDeck.Desktop.Models;

namespace OneDeck.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var portWs = 9720;
        var portHttp = 9721;

        // 解析命令行参数
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--ws-port" && i + 1 < args.Length)
                int.TryParse(args[++i], out portWs);
            if (args[i] == "--http-port" && i + 1 < args.Length)
                int.TryParse(args[++i], out portHttp);
        }

        // 初始化 WinForms
        ApplicationConfiguration.Initialize();

        // 创建主窗体（内部启动 HTTP 服务器和 WebView2）
        var mainForm = new MainForm(portHttp, portWs);

        // 运行消息循环（阻塞直到窗口关闭）
        Application.Run(mainForm);
    }
}
