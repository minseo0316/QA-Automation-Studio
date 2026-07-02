using System.Net;

namespace MyWinFormsApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        // 💡 [SSL/TLS 오류 해결] Discord 웹훅과 같은 최신 HTTPS 서비스와의 보안 연결을 위해
        // 애플리케이션이 TLS 1.2 이상을 사용하도록 명시적으로 설정합니다.
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }    
}
