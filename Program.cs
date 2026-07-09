using System.Net;

namespace MyWinFormsApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        // Discord Webhook 같은 최신 HTTPS 서비스와 안전하게 연결하기 위해 TLS 1.2 이상을 사용합니다.
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}
