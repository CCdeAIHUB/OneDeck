using System;
using System.Windows.Forms;
using OneDesk.Desktop.Models;

namespace OneDesk.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var portWs = 9720;

        // 解析命令行参数
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--ws-port" && i + 1 < args.Length)
                int.TryParse(args[++i], out portWs);
        }

        // 初始化 WinForms
        ApplicationConfiguration.Initialize();

        // 创建主窗体（前端资源以文件方式加载，无需 HTTP 服务器）
        var mainForm = new MainForm(portWs);

        // 运行消息循环（阻塞直到窗口关闭）
        Application.Run(mainForm);
    }
}
