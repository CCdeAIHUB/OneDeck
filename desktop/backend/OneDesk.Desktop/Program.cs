using OneDeck.Desktop.Models;

namespace OneDeck.Desktop;

class Program
{
    static async Task Main(string[] args)
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

        var app = new AppHost();
        await app.StartAsync(portWs, portHttp);
    }
}
