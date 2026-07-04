using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MyWinFormsApp;

public partial class Form1
{
    private FileSystemWatcher? runtimeEvidenceWatcher;
    private readonly ConcurrentDictionary<string, byte> processedRuntimeEvidence =
        new(StringComparer.OrdinalIgnoreCase);

    private void SetupRuntimeEvidenceWatcher()
    {
        runtimeEvidenceWatcher?.Dispose();
        runtimeEvidenceWatcher = null;
        processedRuntimeEvidence.Clear();

        if (string.IsNullOrWhiteSpace(PathManager.UnityProjectPath)) return;

        try
        {
            string runtimeDirectory = Path.Combine(
                PathManager.UnityProjectPath, "TestResults", "RuntimeMonitoring");
            Directory.CreateDirectory(runtimeDirectory);

            foreach (string existingReport in Directory.GetFiles(runtimeDirectory, "Runtime_Bug_*.json"))
            {
                processedRuntimeEvidence.TryAdd(existingReport, 0);
            }

            runtimeEvidenceWatcher = new FileSystemWatcher(runtimeDirectory, "Runtime_Bug_*.json")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            runtimeEvidenceWatcher.Created += OnRuntimeEvidenceCreated;
            runtimeEvidenceWatcher.Renamed += OnRuntimeEvidenceCreated;
            liveLogTextBox.AppendText($"{Environment.NewLine}[Runtime] 실시간 증거 감시 활성: {runtimeDirectory}");
        }
        catch (Exception ex)
        {
            liveLogTextBox.AppendText($"{Environment.NewLine}[Runtime] 감시 시작 실패: {ex.Message}");
        }
    }

    private async void OnRuntimeEvidenceCreated(object sender, FileSystemEventArgs e)
    {
        if (!processedRuntimeEvidence.TryAdd(e.FullPath, 0)) return;

        RuntimeQaIssue? issue = null;
        for (int attempt = 0; attempt < 10 && issue == null; attempt++)
        {
            try
            {
                await Task.Delay(150);
                using FileStream stream = new FileStream(
                    e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                issue = await JsonSerializer.DeserializeAsync<RuntimeQaIssue>(stream);
            }
            catch (IOException)
            {
            }
            catch (JsonException)
            {
            }
        }

        if (issue == null)
        {
            processedRuntimeEvidence.TryRemove(e.FullPath, out _);
            return;
        }

        string screenshotPath = issue.screenshotPath;
        if (string.IsNullOrWhiteSpace(screenshotPath) || !File.Exists(screenshotPath))
        {
            screenshotPath = Path.ChangeExtension(e.FullPath, ".png");
        }

        ShowRuntimeEvidence(issue, screenshotPath);
        await SendRuntimeEvidenceToDiscordAsync(issue, screenshotPath, e.FullPath);
    }

    private void ShowRuntimeEvidence(RuntimeQaIssue issue, string screenshotPath)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => ShowRuntimeEvidence(issue, screenshotPath)));
            return;
        }

        string sourceLocation = ResolveRuntimeSourceLocation(issue.stackTrace);
        infoBadge.Visible = true;
        infoBadge.BadgeText = "❌ 실시간 런타임 결함 감지";
        lblTotalCount.Text = "TOTAL: 1";
        lblFailedCount.Text = "FAILED: 1";
        reportTextBox.Text =
            $"실시간 런타임 결함{Environment.NewLine}{Environment.NewLine}" +
            $"씬  {issue.scene}{Environment.NewLine}" +
            $"분류  {issue.category}{Environment.NewLine}" +
            $"원인  {issue.message}{Environment.NewLine}" +
            $"위치  {sourceLocation}{Environment.NewLine}{Environment.NewLine}" +
            issue.stackTrace;

        if (File.Exists(screenshotPath))
        {
            try
            {
                using FileStream stream = new FileStream(
                    screenshotPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using Image sourceImage = Image.FromStream(stream);
                picScreenshot.Image?.Dispose();
                picScreenshot.Image = new Bitmap(sourceImage);
                picScreenshot.Visible = true;
            }
            catch (Exception ex)
            {
                liveLogTextBox.AppendText($"{Environment.NewLine}[Runtime] 이미지 표시 실패: {ex.Message}");
            }
        }
    }

    private async Task SendRuntimeEvidenceToDiscordAsync(
        RuntimeQaIssue issue, string screenshotPath, string reportPath)
    {
        string webhookUrl = !string.IsNullOrWhiteSpace(PathManager.AutoModeDiscordWebhookUrl)
            ? PathManager.AutoModeDiscordWebhookUrl
            : PathManager.DiscordWebhookUrl;

        if (string.IsNullOrWhiteSpace(webhookUrl) || webhookUrl.Contains("YOUR_WEBHOOK_URL"))
        {
            AppendRuntimeLog("Discord Webhook URL이 설정되지 않았습니다.");
            return;
        }

        List<FileStream> streams = new List<FileStream>();
        try
        {
            using MultipartFormDataContent multipart = new MultipartFormDataContent();
            string sourceLocation = ResolveRuntimeSourceLocation(issue.stackTrace);
            string message = issue.message.Length > 700 ? issue.message[..700] + "..." : issue.message;
            var payload = new
            {
                username = "QA Runtime Monitor",
                content =
                    $"❌ **[실시간 런타임 결함 감지]**{Environment.NewLine}" +
                    $"- 씬: {issue.scene}{Environment.NewLine}" +
                    $"- 분류: {issue.category}{Environment.NewLine}" +
                    $"- 원인: {message}{Environment.NewLine}" +
                    $"- 위치: {sourceLocation}{Environment.NewLine}" +
                    "- Unity 일반 Play 중 자동 수집된 게임 화면입니다."
            };

            multipart.Add(
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
                "payload_json");
            AddDiscordAttachment(multipart, streams, screenshotPath, "file1", "image/png");
            AddDiscordAttachment(multipart, streams, reportPath, "file2", "application/json");

            using HttpResponseMessage response = await httpClient.PostAsync(webhookUrl, multipart);
            response.EnsureSuccessStatusCode();
            AppendRuntimeLog($"Discord 전송 완료: {Path.GetFileName(screenshotPath)}");
        }
        catch (Exception ex)
        {
            AppendRuntimeLog($"Discord 전송 실패: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            foreach (FileStream stream in streams) stream.Dispose();
        }
    }

    private void AppendRuntimeLog(string message)
    {
        if (IsDisposed || Disposing) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => AppendRuntimeLog(message)));
            return;
        }
        liveLogTextBox.AppendText($"{Environment.NewLine}[Runtime] {message}");
    }

    private static string ResolveRuntimeSourceLocation(string stackTrace)
    {
        Match match = Regex.Match(
            stackTrace ?? string.Empty,
            @"\(at (?<path>Assets[/\\][^:()]+):(?<line>\d+)\)");
        if (!match.Success) return "런타임 로그에 소스 위치 없음";
        return $"{match.Groups["path"].Value.Replace('/', '\\')}  (line {match.Groups["line"].Value})";
    }

    private sealed class RuntimeQaIssue
    {
        public string timestamp { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string stackTrace { get; set; } = string.Empty;
        public string scene { get; set; } = string.Empty;
        public string screenshotPath { get; set; } = string.Empty;
    }
}
