using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace MyWinFormsApp;

public partial class Form1
{
    private async void btnPerformanceAudit_Click(object? sender, EventArgs e)
    {
        await RunPerformanceAuditAsync();
    }

    private async Task RunPerformanceAuditAsync()
    {
        if (string.IsNullOrWhiteSpace(PathManager.UnityProjectPath)
            || !Directory.Exists(PathManager.UnityProjectPath))
        {
            MessageBox.Show("Unity 프로젝트 경로를 먼저 설정해 주세요.", "성능 진단", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnPerformanceAudit.Enabled = false;
        infoBadge.Visible = true;
        infoBadge.BadgeText = "성능 진단 중: 코드 구조와 런타임 데이터를 분석합니다.";
        reportTextBox.Text = "성능 진단을 준비 중입니다...\r\nUpdate 구조, 오브젝트 생성/삭제, UI Canvas, 오브젝트 풀링 사용 여부를 확인합니다.";
        liveLogTextBox.AppendText($"{Environment.NewLine}[Performance] 성능 진단 시작");

        try
        {
            Directory.CreateDirectory(PathManager.OutputDirectory);
            PerformanceAuditReport report = await Task.Run(() =>
                PerformanceAnalyzer.Analyze(PathManager.UnityProjectPath, PathManager.OutputDirectory));

            string textReport = PerformanceReportRenderer.ToText(report);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string textPath = Path.Combine(PathManager.OutputDirectory, $"Performance_Audit_{stamp}.txt");
            string htmlPath = Path.Combine(PathManager.OutputDirectory, $"Performance_Audit_{stamp}.html");
            File.WriteAllText(textPath, textReport, Encoding.UTF8);
            File.WriteAllText(htmlPath, PerformanceReportRenderer.ToHtml(report), Encoding.UTF8);

            reportTextBox.Text = textReport + Environment.NewLine + $"HTML 보고서: {htmlPath}";
            infoBadge.BadgeText = $"성능 진단 완료: 개선 후보 {report.Findings.Count}건";
            lblTotalCount.Text = $"PERF: {report.Findings.Count}";
            lblPassedCount.Text = $"POOL: {report.PoolingFiles.Count}";
            lblFailedCount.Text = $"HOT: {report.HighRiskCount}";
            picScreenshot.Visible = false;
            SaveReportToHistory(textReport);
            await SendPerformanceReportToDiscordAsync(report, textPath, htmlPath);
        }
        catch (Exception ex)
        {
            reportTextBox.Text = "성능 진단 중 오류가 발생했습니다: " + ex.Message;
            liveLogTextBox.AppendText($"{Environment.NewLine}[Performance] 진단 실패: {ex.Message}");
        }
        finally
        {
            btnPerformanceAudit.Enabled = true;
        }
    }

    private async Task SendPerformanceReportToDiscordAsync(PerformanceAuditReport report, string textPath, string htmlPath)
    {
        string webhookUrl = !string.IsNullOrWhiteSpace(PathManager.AutoModeDiscordWebhookUrl)
            ? PathManager.AutoModeDiscordWebhookUrl
            : PathManager.DiscordWebhookUrl;

        if (string.IsNullOrWhiteSpace(webhookUrl) || webhookUrl.Contains("YOUR_WEBHOOK_URL"))
        {
            liveLogTextBox.AppendText($"{Environment.NewLine}[Performance] Discord Webhook URL이 설정되지 않아 로컬 리포트만 생성했습니다.");
            return;
        }

        List<FileStream> streams = new List<FileStream>();
        try
        {
            using MultipartFormDataContent multipart = new MultipartFormDataContent();
            string runtimeLine = report.RuntimeSnapshot == null
                ? "런타임 스냅샷 없음"
                : $"평균 FPS {report.RuntimeSnapshot.averageFps:F1}, 최저 FPS {report.RuntimeSnapshot.minFps:F1}, 메모리 {report.RuntimeSnapshot.gcMemoryMb:F1}MB";

            var payload = new
            {
                username = "QA Performance Auditor",
                content =
                    $"📊 **[Unity 성능 진단 리포트]**{Environment.NewLine}" +
                    $"- 프로젝트: {Path.GetFileName(PathManager.UnityProjectPath)}{Environment.NewLine}" +
                    $"- 개선 후보: {report.Findings.Count}건 / 고위험: {report.HighRiskCount}건{Environment.NewLine}" +
                    $"- 런타임 데이터: {runtimeLine}{Environment.NewLine}" +
                    "- 첨부된 TXT/HTML 보고서에서 파일 위치와 개선 제안을 확인하세요."
            };

            multipart.Add(new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), "payload_json");
            AddDiscordAttachment(multipart, streams, textPath, "file1", "text/plain");
            AddDiscordAttachment(multipart, streams, htmlPath, "file2", "text/html");
            using HttpResponseMessage response = await httpClient.PostAsync(webhookUrl, multipart);
            response.EnsureSuccessStatusCode();
            liveLogTextBox.AppendText($"{Environment.NewLine}[Performance] Discord 성능 리포트 전송 완료");
        }
        catch (Exception ex)
        {
            liveLogTextBox.AppendText($"{Environment.NewLine}[Performance] Discord 전송 실패: {ex.InnerException?.Message ?? ex.Message}");
        }
        finally
        {
            foreach (FileStream stream in streams) stream.Dispose();
        }
    }
}

internal static class PerformanceAnalyzer
{
    private static readonly Regex MethodRegex = new(@"\b(?:public|private|protected|internal)?\s*(?:virtual|override|sealed|static)?\s*void\s+(Update|FixedUpdate|LateUpdate)\s*\(", RegexOptions.Compiled);
    private static readonly Regex PoolRegex = new(@"\b(pool|pooling|ObjectPool|IObjectPool)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static PerformanceAuditReport Analyze(string unityProjectPath, string outputDirectory)
    {
        string scriptsPath = Path.Combine(unityProjectPath, "Assets", "Scripts");
        PerformanceAuditReport report = new PerformanceAuditReport
        {
            ProjectPath = unityProjectPath,
            GeneratedAt = DateTime.Now,
            RuntimeSnapshot = LoadLatestRuntimeSnapshot(unityProjectPath)
        };

        if (!Directory.Exists(scriptsPath))
        {
            report.Findings.Add(new PerformanceFinding(
                "High", "ScriptsMissing", unityProjectPath, 0,
                "Assets/Scripts 폴더를 찾지 못했습니다.",
                "Unity 프로젝트 경로가 올바른지 확인하세요."));
            return report;
        }

        foreach (string filePath in Directory.EnumerateFiles(scriptsPath, "*.cs", SearchOption.AllDirectories))
        {
            if (filePath.Contains(Path.DirectorySeparatorChar + "QA" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(unityProjectPath, filePath);
            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch (IOException)
            {
                continue;
            }

            string fullText = string.Join('\n', lines);
            if (PoolRegex.IsMatch(fullText))
            {
                report.PoolingFiles.Add(relativePath);
            }

            AnalyzeLifecycleMethods(report, relativePath, lines);
            AnalyzeInstantiation(report, relativePath, lines);
            AnalyzeUiCanvasPatterns(report, relativePath, lines);
        }

        if (report.PoolingFiles.Count == 0)
        {
            report.Findings.Add(new PerformanceFinding(
                "Medium", "ObjectPooling", "Assets/Scripts", 0,
                "오브젝트 풀링 사용 흔적을 찾지 못했습니다.",
                "피격 이펙트, 투사체, 몬스터처럼 반복 생성되는 오브젝트는 풀링 후보로 분류하세요."));
        }

        report.Findings.Sort((a, b) => SeverityRank(b.Severity).CompareTo(SeverityRank(a.Severity)));
        return report;
    }

    private static void AnalyzeLifecycleMethods(PerformanceAuditReport report, string relativePath, string[] lines)
    {
        for (int index = 0; index < lines.Length; index++)
        {
            Match methodMatch = MethodRegex.Match(lines[index]);
            if (!methodMatch.Success) continue;

            report.UpdateMethodCount++;
            List<(int lineNumber, string text)> body = ExtractMethodBody(lines, index);
            foreach ((int lineNumber, string text) in body)
            {
                if (text.Contains("FindGameObjectsWithTag", StringComparison.Ordinal)
                    || text.Contains("FindObjectsByType", StringComparison.Ordinal)
                    || text.Contains("FindObject", StringComparison.Ordinal))
                {
                    report.Findings.Add(new PerformanceFinding(
                        "High", "FindInUpdate", relativePath, lineNumber,
                        $"{methodMatch.Groups[1].Value} 안에서 Find 계열 API를 호출합니다.",
                        "씬 전체 검색은 비용이 커서 캐싱, 이벤트 등록, Spawner/Manager 목록 관리 방식으로 바꾸는 것을 권장합니다."));
                }

                if (text.Contains("GetComponent", StringComparison.Ordinal))
                {
                    report.Findings.Add(new PerformanceFinding(
                        "Medium", "GetComponentInUpdate", relativePath, lineNumber,
                        $"{methodMatch.Groups[1].Value} 안에서 GetComponent 계열 API를 호출합니다.",
                        "반복 접근하는 컴포넌트는 Awake/Start에서 캐싱해 프레임 비용을 줄이세요."));
                }

                if (text.Contains("Instantiate(", StringComparison.Ordinal) || text.Contains("Destroy(", StringComparison.Ordinal))
                {
                    report.Findings.Add(new PerformanceFinding(
                        "High", "CreateDestroyInUpdate", relativePath, lineNumber,
                        $"{methodMatch.Groups[1].Value} 안에서 Instantiate/Destroy가 호출됩니다.",
                        "프레임 중 생성/삭제가 반복되면 GC와 프레임 드랍이 생길 수 있으니 오브젝트 풀링을 우선 검토하세요."));
                }
            }
        }
    }

    private static void AnalyzeInstantiation(PerformanceAuditReport report, string relativePath, string[] lines)
    {
        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];
            if (line.Contains("Instantiate(", StringComparison.Ordinal))
            {
                report.InstantiateCount++;
            }

            if (line.Contains("Destroy(", StringComparison.Ordinal))
            {
                report.DestroyCount++;
            }
        }

        if (report.InstantiateCount + report.DestroyCount > 0
            && !report.PoolingFiles.Contains(relativePath)
            && (relativePath.Contains("Effect", StringComparison.OrdinalIgnoreCase)
                || relativePath.Contains("Projectile", StringComparison.OrdinalIgnoreCase)
                || relativePath.Contains("Spawner", StringComparison.OrdinalIgnoreCase)))
        {
            report.Findings.Add(new PerformanceFinding(
                "Medium", "PoolingCandidate", relativePath, 0,
                "반복 생성/삭제 가능성이 있는 파일입니다.",
                "이펙트, 투사체, 스폰 오브젝트는 풀링 적용 여부를 Inspector와 코드에서 함께 확인하세요."));
        }
    }

    private static void AnalyzeUiCanvasPatterns(PerformanceAuditReport report, string relativePath, string[] lines)
    {
        bool isUiFile = relativePath.Contains($"{Path.DirectorySeparatorChar}UI{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("UI", StringComparison.OrdinalIgnoreCase)
            || lines.Any(line => line.Contains("Canvas", StringComparison.Ordinal) || line.Contains("TextMeshProUGUI", StringComparison.Ordinal));

        if (!isUiFile) return;
        report.UiScriptCount++;

        int setActiveCount = lines.Count(line => line.Contains(".SetActive(", StringComparison.Ordinal));
        if (setActiveCount >= 4)
        {
            report.Findings.Add(new PerformanceFinding(
                "Medium", "UiSetActiveBurst", relativePath, 0,
                $"UI 스크립트에서 SetActive 호출이 {setActiveCount}회 발견되었습니다.",
                "큰 Canvas 전체를 자주 켜고 끄면 Canvas rebuild 비용이 커질 수 있습니다. 정적/동적 Canvas 분리와 CanvasGroup 사용을 검토하세요."));
        }
    }

    private static List<(int lineNumber, string text)> ExtractMethodBody(string[] lines, int startIndex)
    {
        List<(int lineNumber, string text)> body = new();
        int depth = 0;
        bool entered = false;

        for (int index = startIndex; index < lines.Length; index++)
        {
            string line = lines[index];
            if (line.Contains('{'))
            {
                depth += line.Count(ch => ch == '{');
                entered = true;
            }

            if (entered)
            {
                body.Add((index + 1, line.Trim()));
            }

            if (line.Contains('}'))
            {
                depth -= line.Count(ch => ch == '}');
                if (entered && depth <= 0) break;
            }
        }

        return body;
    }

    private static RuntimePerformanceSnapshot? LoadLatestRuntimeSnapshot(string unityProjectPath)
    {
        string directory = Path.Combine(unityProjectPath, "TestResults", "RuntimeMonitoring");
        if (!Directory.Exists(directory)) return null;

        string? latest = Directory.GetFiles(directory, "Performance_Snapshot_*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
        if (latest == null) return null;

        try
        {
            string json = File.ReadAllText(latest);
            RuntimePerformanceSnapshot? snapshot = JsonSerializer.Deserialize<RuntimePerformanceSnapshot>(json);
            if (snapshot != null) snapshot.sourcePath = latest;
            return snapshot;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Performance snapshot load failed: " + ex.Message);
            return null;
        }
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "High" => 3,
        "Medium" => 2,
        _ => 1
    };
}

internal static class PerformanceReportRenderer
{
    public static string ToText(PerformanceAuditReport report)
    {
        StringBuilder builder = new();
        builder.AppendLine("========================================");
        builder.AppendLine("[Unity 성능 진단 리포트]");
        builder.AppendLine($"- 생성 시각: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"- 프로젝트: {report.ProjectPath}");
        builder.AppendLine($"- Update/FixedUpdate/LateUpdate 메서드: {report.UpdateMethodCount}개");
        builder.AppendLine($"- UI 관련 스크립트: {report.UiScriptCount}개");
        builder.AppendLine($"- Instantiate 호출: {report.InstantiateCount}회 / Destroy 호출: {report.DestroyCount}회");
        builder.AppendLine($"- 오브젝트 풀링 흔적: {report.PoolingFiles.Count}개 파일");
        builder.AppendLine($"- 개선 후보: {report.Findings.Count}건 / 고위험: {report.HighRiskCount}건");
        builder.AppendLine("========================================");
        builder.AppendLine();

        if (report.RuntimeSnapshot == null)
        {
            builder.AppendLine("[런타임 성능 데이터]");
            builder.AppendLine("- 최신 Performance_Snapshot 파일이 없습니다.");
            builder.AppendLine("- Unity Play 모드에서 RuntimeQaPerformanceMonitor가 5초 이상 실행되면 FPS/메모리 데이터가 함께 표시됩니다.");
        }
        else
        {
            RuntimePerformanceSnapshot s = report.RuntimeSnapshot;
            builder.AppendLine("[런타임 성능 데이터]");
            builder.AppendLine($"- 씬: {s.scene}");
            builder.AppendLine($"- 평균 FPS: {s.averageFps:F1}");
            builder.AppendLine($"- 최저 FPS: {s.minFps:F1}");
            builder.AppendLine($"- 최대 프레임 시간: {s.maxFrameMs:F1}ms");
            builder.AppendLine($"- 관리 메모리: {s.gcMemoryMb:F1}MB");
            builder.AppendLine($"- Transform: {s.transformCount}개 / Canvas: {s.canvasCount}개 / Camera: {s.cameraCount}개");
            builder.AppendLine($"- 원본: {s.sourcePath}");
        }

        builder.AppendLine();
        builder.AppendLine("[오브젝트 풀링 확인]");
        if (report.PoolingFiles.Count == 0)
        {
            builder.AppendLine("- 풀링 사용 파일을 찾지 못했습니다.");
        }
        else
        {
            foreach (string file in report.PoolingFiles.Take(10))
            {
                builder.AppendLine("- " + file);
            }
        }

        builder.AppendLine();
        builder.AppendLine("[상세 개선 후보]");
        if (report.Findings.Count == 0)
        {
            builder.AppendLine("- 자동 분석에서 즉시 조치할 성능 위험 패턴을 찾지 못했습니다.");
        }
        else
        {
            int index = 1;
            foreach (PerformanceFinding finding in report.Findings.Take(30))
            {
                string location = finding.LineNumber > 0 ? $"{finding.FilePath} (line {finding.LineNumber})" : finding.FilePath;
                builder.AppendLine($"{index:00}. [{finding.Severity}] {finding.Rule}");
                builder.AppendLine($"    위치  {location}");
                builder.AppendLine($"    내용  {finding.Message}");
                builder.AppendLine($"    제안  {finding.Recommendation}");
                builder.AppendLine();
                index++;
            }
        }

        return builder.ToString();
    }

    public static string ToHtml(PerformanceAuditReport report)
    {
        string safeText = HttpUtility.HtmlEncode(ToText(report));
        return $$"""
<!doctype html>
<html lang="ko">
<head>
<meta charset="utf-8">
<title>Unity Performance Audit</title>
<style>
body{margin:0;background:#0d1117;color:#d6deeb;font-family:Segoe UI,Arial,sans-serif}
main{max-width:1040px;margin:0 auto;padding:36px}
h1{font-size:28px;margin:0 0 14px}
.summary{display:grid;grid-template-columns:repeat(4,1fr);gap:12px;margin:20px 0}
.card{background:#161b22;border:1px solid #30363d;border-radius:8px;padding:16px}
.value{font-size:26px;font-weight:700;color:#58a6ff}
pre{white-space:pre-wrap;background:#111827;border:1px solid #30363d;border-radius:8px;padding:20px;line-height:1.55}
</style>
</head>
<body>
<main>
<h1>Unity Performance Audit</h1>
<div class="summary">
<div class="card"><div>개선 후보</div><div class="value">{{report.Findings.Count}}</div></div>
<div class="card"><div>고위험</div><div class="value">{{report.HighRiskCount}}</div></div>
<div class="card"><div>Update 계열</div><div class="value">{{report.UpdateMethodCount}}</div></div>
<div class="card"><div>풀링 파일</div><div class="value">{{report.PoolingFiles.Count}}</div></div>
</div>
<pre>{{safeText}}</pre>
</main>
</body>
</html>
""";
    }
}

internal sealed class PerformanceAuditReport
{
    public string ProjectPath { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int UpdateMethodCount { get; set; }
    public int UiScriptCount { get; set; }
    public int InstantiateCount { get; set; }
    public int DestroyCount { get; set; }
    public RuntimePerformanceSnapshot? RuntimeSnapshot { get; set; }
    public List<string> PoolingFiles { get; } = new();
    public List<PerformanceFinding> Findings { get; } = new();
    public int HighRiskCount => Findings.Count(finding => finding.Severity == "High");
}

internal sealed record PerformanceFinding(
    string Severity,
    string Rule,
    string FilePath,
    int LineNumber,
    string Message,
    string Recommendation);

internal sealed class RuntimePerformanceSnapshot
{
    public string timestamp { get; set; } = string.Empty;
    public string scene { get; set; } = string.Empty;
    public float averageFps { get; set; }
    public float minFps { get; set; }
    public float maxFrameMs { get; set; }
    public float gcMemoryMb { get; set; }
    public int transformCount { get; set; }
    public int canvasCount { get; set; }
    public int cameraCount { get; set; }
    public string sourcePath { get; set; } = string.Empty;
}
