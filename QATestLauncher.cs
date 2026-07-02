using System;
using System.Diagnostics;
using System.IO;
using MyWinFormsApp;

class QATestLauncher
{
    public static void RunUnityTest()
    {
        string unityPath = PathManager.UnityEditorPath;
        string projectPath = PathManager.UnityProjectPath;

        if (string.IsNullOrWhiteSpace(unityPath) || string.IsNullOrWhiteSpace(projectPath))
        {
            Console.WriteLine("Unity Editor 경로 또는 프로젝트 경로가 설정되지 않았습니다.");
            return;
        }
        
        Directory.CreateDirectory(PathManager.OutputDirectory);
        string reportPath = Path.Combine(PathManager.OutputDirectory, "Result.xml");
        string logPath = Path.Combine(PathManager.OutputDirectory, "Unity_QA_Log.txt");

        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        
        // 💡 안전한 덮어쓰기를 위해 기존 파일 정리 시 try-catch 적용
        try
        {
            if (File.Exists(reportPath)) File.Delete(reportPath);
            if (File.Exists(logPath)) File.Delete(logPath);
        }
        catch (IOException) { }

        // 💡 [수정 완료] Unity 6의 강제 화면 켬을 방지하기 위해 -noGraphics 옵션을 결합했습니다.
        string arguments = $"-batchmode -noGraphics -projectPath \"{projectPath}\" -runTests -testResults \"{reportPath}\" -testPlatform EditMode -logFile \"{logPath}\"";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = unityPath,
            Arguments = arguments,
            UseShellExecute = false, 
            CreateNoWindow = true,  
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            Console.WriteLine("유니티 백그라운드 테스트 프로세스를 안전하게 호출합니다...");
            using (Process process = Process.Start(startInfo))
            {
                if (process != null)
                {
                    process.WaitForExit(); 
                }
                else
                {
                    Console.WriteLine("❌ 에러: 프로세스를 시작할 수 없습니다.");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ .NET 자체 실행 에러 발생: {ex.Message}");
            return;
        }

        Console.WriteLine("테스트 프로세스 종료! 리포트 분석 중...");
        ParseTestResult(reportPath, logPath);
    }

    public static void ParseTestResult(string xmlPath, string logPath)
    {
        if (!File.Exists(xmlPath)) 
        { 
            Console.WriteLine("❌ 에러: 리포트 XML 파일이 최종적으로 생성되지 않았습니다.");
            return; 
        }

        string xmlContent = File.ReadAllText(xmlPath);
        if (xmlContent.Contains("result=\"Passed\"") && !xmlContent.Contains("result=\"Failed\""))
        {
            Console.WriteLine("★ [QA 결과] 모든 테스트 통과 (SUCCESS) ★");
        }
        else
        {
            Console.WriteLine("‼ [QA 결과] 일부 테스트 실패 (FAILED) ‼");
        }
    }
}
