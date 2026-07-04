#define DEBUG
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using MyWinFormsApp.Controls;

namespace MyWinFormsApp;

public partial class Form1 : Form
{
	private RoundedButton runTestButton = null;

	private NeonBadgePanel infoBadge = null;

	private ModernProgressBar automationProgressBar = null;

	private DarkLogTextBox reportTextBox = null;

	private DarkLogTextBox liveLogTextBox = null;

	private System.Windows.Forms.Timer progressTimer = null;

	private FileSystemWatcher? logFileWatcher;

	private TextBox txtUnityPath = null;

	private TextBox txtProjectPath = null;

	private TextBox txtOutputPath = null;

	private static readonly HttpClient httpClient = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(30)
	};

	private ListBox historyListBox = null;

	private readonly string historyFolderPath = Path.Combine(PathManager.OutputDirectory, "History");

	private RoundedButton btnCopyReport = null;

	private Label copyNotificationLabel = null;

	private System.Windows.Forms.Timer copyNotificationTimer = null;

	private PictureBox picScreenshot = null;

	private Label lblTotalCount = null;

	private Label lblPassedCount = null;

	private Label lblFailedCount = null;

	private RoundedButton btnToggleAutoMode = null;

	private NumericUpDown numInterval = null;

	private Label lblStatusNotice = null;

	private System.Windows.Forms.Timer autoModeTimer = null;

	private bool isAutoModeActive = false;

	private DateTime nextAutoRunTime;

	private TextBox txtDiscordWebhookUrl = null;

	private TextBox txtAutoModeDiscordWebhookUrl = null;

	private TextBox txtScreenshotPath = null;


	public Form1()
	{
		InitializeComponent();
		SetupAdvancedUI();
	}

	private async void button1_Click(object? sender, EventArgs? e)
	{
		await RunTestAsync(isAutoModeRun: false);
	}

	private async Task RunTestAsync(bool isAutoModeRun)
	{
		Directory.CreateDirectory(PathManager.OutputDirectory);
		Directory.CreateDirectory(PathManager.ScreenshotDirectory);
		string reportPath = PathManager.XmlReportPath;
		string logPath = PathManager.UnityLogPath;
		string finalTxtReportPath = PathManager.FinalReportPath;
		string screenshotFileName = $"Bug_Screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
		string fullScreenshotPath = Path.Combine(PathManager.ScreenshotDirectory, screenshotFileName);
		string frameDirectory = Path.Combine(PathManager.ScreenshotDirectory, Path.GetFileNameWithoutExtension(screenshotFileName) + "_frames");
		Directory.CreateDirectory(frameDirectory);
		try
		{
			if (File.Exists(reportPath))
			{
				File.Delete(reportPath);
			}
			if (File.Exists(logPath))
			{
				File.Delete(logPath);
			}
			if (File.Exists(finalTxtReportPath))
			{
				File.Delete(finalTxtReportPath);
			}
		}
		catch (IOException)
		{
		}
		runTestButton.Enabled = false;
		picScreenshot.Image = null;
		picScreenshot.Visible = false;
		lblTotalCount.Text = "TOTAL: 0";
		lblPassedCount.Text = "PASSED: 0";
		lblFailedCount.Text = "FAILED: 0";
		automationProgressBar.Value = 0;
		progressTimer.Start();
		infoBadge.Visible = true;
		infoBadge.BadgeText = "⏳ 분석 중: 유니티 백그라운드 테스트를 가동하는 중...";
		reportTextBox.Text = "유니티 백그라운드 엔진이 소스코드를 컴파일하고 유닛 테스트를 연산 중입니다...\r\n이 작업 도중 창을 움직이거나 다른 작업을 하셔도 툴이 멈추지 않습니다.";
		liveLogTextBox.Text = "";
		string arguments = $"-batchmode -projectPath \"{PathManager.UnityProjectPath}\" -runTests -testResults \"{reportPath}\" -testPlatform PlayMode -logFile \"{logPath}\" -screenshotPath \"{fullScreenshotPath}\" -evidenceFramesPath \"{frameDirectory}\"";
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = PathManager.UnityEditorPath,
			Arguments = arguments,
			UseShellExecute = false,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden
		};
		bool isSuccess = false;
		try
		{
			await Task.Run(delegate
			{
				using Process process = Process.Start(startInfo);
				process?.WaitForExit();
				isSuccess = process != null && process.ExitCode == 0;
			});
			progressTimer.Stop();
			await Task.Delay(500);
			await ProcessAndWriteReports(reportPath, logPath, finalTxtReportPath, isSuccess, fullScreenshotPath, frameDirectory, isAutoModeRun);
		}
		catch (Exception ex2)
		{
			Exception ex3 = ex2;
			progressTimer.Stop();
			reportTextBox.Text = "❌ 연동 중 예외 오류 발생: " + ex3.Message;
			runTestButton.Enabled = true;
		}
	}

	private async Task ProcessAndWriteReports(string xmlPath, string logPath, string finalTxtReportPath, bool processSuccess, string screenshotPath, string frameDirectory, bool isAutoModeRun)
	{
		if (base.InvokeRequired)
		{
			await (Task)Invoke(new Func<string, string, string, bool, string, string, bool, Task>(ProcessAndWriteReports), xmlPath, logPath, finalTxtReportPath, processSuccess, screenshotPath, frameDirectory, isAutoModeRun);
			return;
		}
		if (!File.Exists(xmlPath))
		{
			automationProgressBar.Value = 100;
			infoBadge.BadgeText = "❌ 실패: 결과 파일 생성 누락";
			string errorMessage = "❌ 유니티 프로세스 실행 실패 또는 리포트 생성 실패!\r\n";
			if (File.Exists(logPath))
			{
				try
				{
					string[] logLines = File.ReadAllLines(logPath);
					string relevantError = "로그에서 명확한 에러 원인을 찾지 못했습니다.";
					string[] errorKeywords = new string[5] { "error CS", "Exception:", "failed", "Aborting", "fatal error" };
					string foundError = logLines.Reverse().FirstOrDefault((string line) => !line.Contains("test-case") && errorKeywords.Any((string keyword) => line.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
					if (!string.IsNullOrEmpty(foundError))
					{
						relevantError = foundError;
					}
					errorMessage = errorMessage + "[탐지된 핵심 에러]:\r\n" + relevantError;
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					errorMessage = errorMessage + "[로그 파일 분석 중 오류]: " + ex2.Message;
				}
			}
			reportTextBox.Text = errorMessage;
			await SendDiscordNotificationAsync("0", "0", isSuccess: false, screenshotPath, null, null, isAutoModeRun);
			return;
		}
		try
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(xmlPath);
			XmlNode testRunNode = doc.SelectSingleNode("//test-run");
			string total = testRunNode?.Attributes?["total"]?.Value ?? "0";
			string passed = testRunNode?.Attributes?["passed"]?.Value ?? "0";
			string failed = testRunNode?.Attributes?["failed"]?.Value ?? "0";
			bool isTestSuccess = total != "0" && failed == "0";
			lblTotalCount.Text = "TOTAL: " + total;
			lblPassedCount.Text = "PASSED: " + passed;
			lblFailedCount.Text = "FAILED: " + failed;
			string fileContent;
			string uiDisplayText;
			if (total == "0")
			{
				automationProgressBar.Value = 100;
				infoBadge.BadgeText = "⚠\ufe0f 테스트 없음: 실행된 테스트 케이스가 없습니다.";
				picScreenshot.Visible = false;
				fileContent = "========================================\r\n[QA 자동화 테스트 결과 보고서]\r\n- 판정: 테스트 없음 (NO TESTS)\r\n- 결과 요약: Unity 프로젝트에서 실행할 테스트를 찾지 못했습니다.\r\n========================================\r\n";
				uiDisplayText = fileContent + "\r\n❓ 테스트 스크립트에 [Test] 또는 [UnityTest] 속성이 포함되어 있는지 확인하세요.";
			}
			else if (isTestSuccess)
			{
				automationProgressBar.Value = 100;
				infoBadge.BadgeText = "✔ 모든 시나리오 검증 통과 (SUCCESS)";
				picScreenshot.Visible = false;
				fileContent = "========================================\r\n[QA 자동화 테스트 결과 보고서]\r\n- 판정: 성공 (SUCCESS)\r\n- 결과 요약: 모든 기능 검증 완료, 발견된 결함 없음\r\n" + $"- 테스트 총 개수: {total}개 / 패스: {passed}개\r\n" + "========================================\r\n";
				uiDisplayText = fileContent + "\r\n\ud83c\udf89 시스템 검증 완료: 정상 빌드가 가능한 안전한 상태입니다.";
			}
			else
			{
				automationProgressBar.Value = 100;
				infoBadge.BadgeText = "❌ 결함 발견: 총 " + failed + "개의 결함 검출";
				string fullScreenshotPath = ResolveLatestScreenshotPath(screenshotPath);
				if (File.Exists(fullScreenshotPath))
				{
					try
					{
						using (FileStream stream = new FileStream(fullScreenshotPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						{
							using Image loadedImage = Image.FromStream(stream);
							picScreenshot.Image?.Dispose();
							picScreenshot.Image = new Bitmap(loadedImage);
						}
						picScreenshot.Visible = true;
					}
					catch (Exception ex)
					{
						Exception ex3 = ex;
						Debug.WriteLine("Screenshot load failed: " + ex3.Message);
						picScreenshot.Visible = false;
					}
				}
				fileContent = "========================================\r\n[QA 자동화 테스트 결과 보고서]\r\n- 판정: 위험/결함 발견 (FAILED)\r\n- 발견된 에러 총 개수: " + failed + "개\r\n========================================\r\n\r\n[상세 결함 내역]\r\n";
				XmlNodeList failedTests = doc.SelectNodes("//test-case[@result='Failed']");
				if (failedTests != null)
				{
					int idx = 1;
					foreach (XmlNode testCase in failedTests)
					{
						string rawTestName = testCase.Attributes?["name"]?.Value ?? string.Empty;
						string testName = FormatTestName(rawTestName);
						string failureMessage = FormatFailureMessage(testCase.SelectSingleNode("failure/message")?.InnerText);
						string stackTrace = testCase.SelectSingleNode("failure/stack-trace")?.InnerText ?? "";
						string errorLine = ExtractProjectSourceLocation(stackTrace);
						if (errorLine == "소스 위치 정보 없음")
						{
							errorLine = FindTestMethodSourceLocation(rawTestName);
						}
						fileContent += $"{idx:00}. {testName}\r\n    원인  {failureMessage}\r\n    위치  {errorLine}\r\n\r\n";
						idx++;
					}
				}
				uiDisplayText = fileContent + "\r\n⚠\ufe0f 위 상세 보고서 텍스트를 복사하여 담당 개발자에게 전달해 주세요.";
			}
			File.WriteAllText(finalTxtReportPath, fileContent, Encoding.UTF8);
			reportTextBox.Text = uiDisplayText;
			SaveReportToHistory(fileContent);
			QaEvidence evidence = QaEvidenceBuilder.Build(frameDirectory, PathManager.OutputDirectory, fileContent, total, passed, failed, isAutoModeRun);
			await SendDiscordNotificationAsync(total, failed, isTestSuccess, screenshotPath, evidence.AnimationPath, evidence.HtmlReportPath, isAutoModeRun);
		}
		catch (Exception ex)
		{
			Exception ex4 = ex;
			reportTextBox.Text = "❌ 결과 가공 중 오류: " + ex4.Message;
		}
		finally
		{
			runTestButton.Enabled = true;
		}
	}

	private static string ResolveLatestScreenshotPath(string basePath)
	{
		string directory = Path.GetDirectoryName(basePath) ?? string.Empty;
		if (!Directory.Exists(directory))
		{
			return basePath;
		}

		string fileName = Path.GetFileNameWithoutExtension(basePath);
		string extension = Path.GetExtension(basePath);
		return Directory.GetFiles(directory, fileName + "*" + extension)
			.OrderByDescending(File.GetLastWriteTimeUtc)
			.FirstOrDefault() ?? basePath;
	}

	private static string FormatTestName(string? rawName)
	{
		if (string.IsNullOrWhiteSpace(rawName))
		{
			return "이름을 확인할 수 없는 테스트";
		}

		string name = rawName.Split('(')[0];
		name = name.Replace("_ReproductionScenario", string.Empty).Replace('_', ' ');
		return name.Trim();
	}

	private static string FormatFailureMessage(string? rawMessage)
	{
		if (string.IsNullOrWhiteSpace(rawMessage))
		{
			return "오류 메시지가 기록되지 않았습니다.";
		}

		string[] lines = rawMessage.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
		string message = lines.Select(line => line.Trim())
			.FirstOrDefault(line => !line.StartsWith("Expected:", StringComparison.OrdinalIgnoreCase)
				&& !line.StartsWith("But was:", StringComparison.OrdinalIgnoreCase)
				&& !line.StartsWith("at ", StringComparison.OrdinalIgnoreCase)) ?? lines[0].Trim();
		return message.Length > 220 ? message[..220] + "..." : message;
	}

	private static string ExtractProjectSourceLocation(string stackTrace)
	{
		if (string.IsNullOrWhiteSpace(stackTrace))
		{
			return "소스 위치 정보 없음";
		}

		(string path, string line)? bestMatch = null;
		string[] lines = stackTrace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string rawLine in lines)
		{
			string line = rawLine.Trim();
			foreach (string pattern in new[]
			{
				@"(?:\bin\b|\bat\b)\s+(?<path>.*?\.cs):line\s*(?<line>\d+)",
				@"(?<path>[A-Za-z]:.*?\.cs):line\s*(?<line>\d+)",
				@"(?<path>.*?\.cs)\s*\(at\s+(?<line>\d+)\)"
			})
			{
				Match match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
				if (!match.Success)
				{
					continue;
				}

				string matchPath = NormalizeSourcePath(match.Groups["path"].Value);
				string lineNumber = match.Groups["line"].Value;
				bool isProjectFile = matchPath.Contains("Knight_Shift", StringComparison.OrdinalIgnoreCase)
					|| matchPath.Contains("Assets", StringComparison.OrdinalIgnoreCase);

				if (bestMatch == null || isProjectFile)
				{
					bestMatch = (matchPath, lineNumber);
				}

				if (isProjectFile)
				{
					goto ReturnBestMatch;
				}
			}
		}

ReturnBestMatch:
		if (bestMatch == null)
		{
			return "소스 위치 정보 없음";
		}

		string path = bestMatch.Value.path;
		int assetsIndex = path.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
		if (assetsIndex >= 0)
		{
			path = path[assetsIndex..];
		}
		return $"{path}  (line {bestMatch.Value.line})";
	}

	private static string NormalizeSourcePath(string path)
	{
		return path.Trim().TrimEnd(')', ']', '>', '"').Replace('/', '\\');
	}

	private static string FindTestMethodSourceLocation(string rawTestName)
	{
		string methodName = rawTestName.Split('(')[0].Split('.').LastOrDefault()?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(methodName) || !Directory.Exists(PathManager.UnityProjectPath))
		{
			return "소스 위치 정보 없음";
		}

		string assetsPath = Path.Combine(PathManager.UnityProjectPath, "Assets");
		if (!Directory.Exists(assetsPath)) return "소스 위치 정보 없음";

		Regex declarationPattern = new Regex(@"\b" + Regex.Escape(methodName) + @"\s*\(", RegexOptions.Compiled);
		try
		{
			foreach (string filePath in Directory.EnumerateFiles(assetsPath, "*.cs", SearchOption.AllDirectories))
			{
				int lineNumber = 0;
				foreach (string line in File.ReadLines(filePath))
				{
					lineNumber++;
					if (!declarationPattern.IsMatch(line)) continue;
					string relativePath = Path.GetRelativePath(PathManager.UnityProjectPath, filePath);
					return $"{relativePath}  (line {lineNumber}, 테스트 선언 위치)";
				}
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Test source lookup failed: " + ex.Message);
		}

		return "소스 위치 정보 없음";
	}
	private async Task SendDiscordNotificationAsync(string total, string failed, bool isSuccess, string screenshotPath, string? animationPath, string? htmlReportPath, bool isAutoModeRun)
	{
		string fullScreenshotPath = ResolveLatestScreenshotPath(screenshotPath);
		string targetWebhookUrl = isAutoModeRun ? PathManager.AutoModeDiscordWebhookUrl : PathManager.DiscordWebhookUrl;
		if (string.IsNullOrEmpty(targetWebhookUrl) || targetWebhookUrl.Contains("YOUR_WEBHOOK_URL"))
		{
			Debug.WriteLine("Webhook URL is not set for the current mode.");
			return;
		}
		try
		{
			if (!isSuccess && (File.Exists(fullScreenshotPath) || File.Exists(animationPath) || File.Exists(htmlReportPath)))
			{
				using MultipartFormDataContent multipartContent = new MultipartFormDataContent();
				string message = $"❌ **[인게임 매크로 결함 감지 Alert]** 시스템 결함 발견 (FAILED){Environment.NewLine}- 발견된 결함: {failed}개{Environment.NewLine}⚠\ufe0f 대시보드에서 상세 내용과 스크린샷을 확인하세요.";
				var payload = new
				{
					username = "QA Automation Bot",
					content = message + Environment.NewLine
						+ $"- 실행 모드: {(isAutoModeRun ? "자동 모니터링" : "수동 테스트")}" + Environment.NewLine
						+ "- 첨부된 게임 화면, 짧은 GIF 기록, HTML 보고서를 확인하세요."
				};
				string jsonPayload = JsonSerializer.Serialize(payload);
				multipartContent.Add(new StringContent(jsonPayload, Encoding.UTF8, "application/json"), "payload_json");
				List<FileStream> streams = new List<FileStream>();
				AddDiscordAttachment(multipartContent, streams, fullScreenshotPath, "file1", "image/png");
				AddDiscordAttachment(multipartContent, streams, animationPath, "file2", "image/gif");
				AddDiscordAttachment(multipartContent, streams, htmlReportPath, "file3", "text/html");
				using HttpResponseMessage response = await httpClient.PostAsync(targetWebhookUrl, multipartContent);
				response.EnsureSuccessStatusCode();
				foreach (FileStream stream in streams) stream.Dispose();
			}
			else
			{
				string message2 = ((!isSuccess) ? $"❌ **[QA 결함 감지 Alert]** 시스템 결함 발견 (FAILED){Environment.NewLine}- 발견된 결함: {failed}개{Environment.NewLine}⚠\ufe0f 대시보드에서 파일 주소 및 라인 번호를 확인하세요. (스크린샷 없음)" : $"✔ **[QA 정기 검증 완료]** 모든 시나리오 통과 (SUCCESS){Environment.NewLine}- 총 테스트 개수: {total}개{Environment.NewLine}\ud83c\udf89 현재 빌드 라인이 매우 안전합니다!");
				var payload2 = new
				{
					username = "QA Automation Bot",
					content = message2
				};
				string jsonPayload2 = JsonSerializer.Serialize(payload2);
				StringContent content = new StringContent(jsonPayload2, Encoding.UTF8, "application/json");
				using HttpResponseMessage response = await httpClient.PostAsync(targetWebhookUrl, content);
				response.EnsureSuccessStatusCode();
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Discord Webhook Error: " + ex);
			string reason = ex.InnerException?.Message ?? ex.Message;
			if (liveLogTextBox != null)
			{
				liveLogTextBox.AppendText($"{Environment.NewLine}[Discord] 전송 실패: {reason}");
			}
		}
	}

	private static void AddDiscordAttachment(MultipartFormDataContent content, List<FileStream> streams,
		string? path, string fieldName, string mediaType)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
		FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		streams.Add(stream);
		StreamContent fileContent = new StreamContent(stream);
		fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
		content.Add(fileContent, fieldName, Path.GetFileName(path));
	}

	private void SaveReportToHistory(string reportContent)
	{
		try
		{
			Directory.CreateDirectory(historyFolderPath);
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string historyFilePath = Path.Combine(historyFolderPath, "Report_" + timestamp + ".txt");
			File.WriteAllText(historyFilePath, reportContent, Encoding.UTF8);
			LoadHistoryFiles();
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to save history: " + ex.Message);
		}
	}

	private void LoadHistoryFiles()
	{
		if (historyListBox.InvokeRequired)
		{
			historyListBox.Invoke(LoadHistoryFiles);
			return;
		}
		historyListBox.Items.Clear();
		try
		{
			Directory.CreateDirectory(historyFolderPath);
			string[] files = (from f in Directory.GetFiles(historyFolderPath, "Report_*.txt")
				orderby new FileInfo(f).Name descending
				select f).ToArray();
			string[] array = files;
			foreach (string file in array)
			{
				historyListBox.Items.Add(Path.GetFileName(file));
			}
		}
		catch (Exception ex)
		{
			historyListBox.Items.Add("히스토리 로드 실패");
			Debug.WriteLine("Failed to load history: " + ex.Message);
		}
	}

	private void historyListBox_SelectedIndexChanged(object? sender, EventArgs e)
	{
		if (historyListBox.SelectedItem == null)
		{
			return;
		}
		string fileName = historyListBox.SelectedItem.ToString() ?? "";
		string filePath = Path.Combine(historyFolderPath, fileName);
		try
		{
			if (File.Exists(filePath))
			{
				reportTextBox.Text = File.ReadAllText(filePath, Encoding.UTF8);
			}
		}
		catch (Exception ex)
		{
			reportTextBox.Text = "히스토리 파일을 읽는 중 오류가 발생했습니다: " + ex.Message;
		}
	}

	private void SaveSettingsFromUI()
	{
		PathManager.UnityEditorPath = txtUnityPath.Text;
		PathManager.UnityProjectPath = txtProjectPath.Text;
		PathManager.OutputDirectory = txtOutputPath.Text;
		PathManager.DiscordWebhookUrl = txtDiscordWebhookUrl.Text;
		PathManager.ScreenshotDirectory = txtScreenshotPath.Text;
		PathManager.AutoModeDiscordWebhookUrl = txtAutoModeDiscordWebhookUrl.Text;
		PathManager.SaveSettings();
		SetupRuntimeEvidenceWatcher();
		MessageBox.Show("경로 설정이 저장되었습니다.", "저장 완료", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
	}

	private void LoadSettingsToUI()
	{
		txtUnityPath.Text = PathManager.UnityEditorPath;
		txtProjectPath.Text = PathManager.UnityProjectPath;
		txtOutputPath.Text = PathManager.OutputDirectory;
		txtDiscordWebhookUrl.Text = PathManager.DiscordWebhookUrl;
		txtScreenshotPath.Text = PathManager.ScreenshotDirectory;
		txtAutoModeDiscordWebhookUrl.Text = PathManager.AutoModeDiscordWebhookUrl;
	}

	private void SetupLogFileWatcher()
	{
		try
		{
			string logDirectory = PathManager.OutputDirectory;
			if (!Directory.Exists(logDirectory))
			{
				Directory.CreateDirectory(logDirectory);
			}
			logFileWatcher = new FileSystemWatcher(logDirectory, Path.GetFileName(PathManager.UnityLogPath))
			{
				NotifyFilter = (NotifyFilters.Size | NotifyFilters.LastWrite),
				EnableRaisingEvents = true
			};
			logFileWatcher.Changed += OnLogFileChanged;
		}
		catch (Exception ex)
		{
			liveLogTextBox.Text = "로그 감시자 설정 실패: " + ex.Message;
		}
	}

	private void OnLogFileChanged(object sender, FileSystemEventArgs e)
	{
		try
		{
			string content = "";
			for (int i = 0; i < 3; i++)
			{
				try
				{
					using FileStream fs = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
					using StreamReader sr = new StreamReader(fs);
					content = sr.ReadToEnd();
				}
				catch (IOException)
				{
					Task.Delay(100).Wait();
					continue;
				}
				break;
			}
			if (liveLogTextBox.InvokeRequired)
			{
				liveLogTextBox.Invoke(delegate
				{
					liveLogTextBox.Text = content;
					liveLogTextBox.SelectionStart = liveLogTextBox.Text.Length;
					liveLogTextBox.ScrollToCaret();
				});
			}
		}
		catch
		{
		}
	}

	private void btnToggleAutoMode_Click(object? sender, EventArgs e)
	{
		isAutoModeActive = !isAutoModeActive;
		if (isAutoModeActive)
		{
			UpdateAutoModeButton(isActive: true);
			numInterval.Enabled = false;
			int intervalMinutes = (int)numInterval.Value;
			DateTime now = DateTime.Now;
			int minutesUntilNextRun = intervalMinutes - now.Minute % intervalMinutes;
			nextAutoRunTime = now.AddMinutes(minutesUntilNextRun).AddSeconds(-now.Second);
			if (nextAutoRunTime <= now)
			{
				nextAutoRunTime = nextAutoRunTime.AddMinutes(intervalMinutes);
			}
			lblStatusNotice.Text = $"⏳ 다음 검사: {nextAutoRunTime:HH:mm:ss} 예정";
			TimeSpan delay = nextAutoRunTime - now;
			autoModeTimer.Interval = Math.Max(1000, (int)delay.TotalMilliseconds);
			autoModeTimer.Start();
		}
		else
		{
			autoModeTimer.Stop();
			UpdateAutoModeButton(isActive: false);
			numInterval.Enabled = true;
			lblStatusNotice.Text = "자동 모니터링이 중지되었습니다.";
		}
	}

	private async void autoModeTimer_Tick(object? sender, EventArgs e)
	{
		if (!runTestButton.Enabled)
		{
			Debug.WriteLine("Auto-monitoring tick skipped: A test is already in progress.");
			return;
		}
		if (autoModeTimer.Interval != (int)numInterval.Value * 60 * 1000)
		{
			autoModeTimer.Interval = (int)numInterval.Value * 60 * 1000;
		}
		await RunTestAsync(isAutoModeRun: true);
		if (base.InvokeRequired)
		{
			Invoke(delegate
			{
				lblStatusNotice.Text = $"⏳ 다음 검사: {DateTime.Now.AddMinutes((int)numInterval.Value):HH:mm:ss} 예정";
			});
		}
		else
		{
			lblStatusNotice.Text = $"⏳ 다음 검사: {DateTime.Now.AddMinutes((int)numInterval.Value):HH:mm:ss} 예정";
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		logFileWatcher?.Dispose();
		runtimeEvidenceWatcher?.Dispose();
		copyNotificationTimer?.Dispose();
		autoModeTimer?.Dispose();
		picScreenshot?.Image?.Dispose();
		base.OnFormClosing(e);
	}

	private void UpdateAutoModeButton(bool isActive)
	{
		if (isActive)
		{
			btnToggleAutoMode.Text = "■ 모니터링 중지";
			btnToggleAutoMode.AnimateTo(UiTheme.Error);
		}
		else
		{
			btnToggleAutoMode.Text = "▶ 자동 모니터링 시작";
			btnToggleAutoMode.AnimateTo(UiTheme.Secondary);
		}
	}

	private void SetupAdvancedUI()
	{
		base.Size = new Size(1280, 820);
		MinimumSize = new Size(1120, 760);
		Text = "QA Automation Studio";
		base.StartPosition = FormStartPosition.CenterScreen;
		BackColor = UiTheme.Background;
		Panel leftPanel = new Panel
		{
			Dock = DockStyle.Left,
			Width = 280,
			Padding = new Padding(0),
			BackColor = UiTheme.CardBackground
		};
		Label titleLabel = new Label
		{
			Text = "⚙\ufe0f 설정",
			Font = new Font("Segoe UI Semibold", 17f, FontStyle.Bold),
			ForeColor = UiTheme.TextPrimary,
			Location = new Point(24, 22),
			Size = new Size(220, 32)
		};
		leftPanel.Controls.Add(titleLabel);
		txtUnityPath = CreatePathInput(leftPanel, "Unity Editor", 85, delegate
		{
			using OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "Unity Editor|Unity.exe",
				Title = "Unity.exe 선택"
			};
			if (openFileDialog.ShowDialog() == DialogResult.OK)
			{
				txtUnityPath.Text = openFileDialog.FileName;
			}
		});
		txtProjectPath = CreatePathInput(leftPanel, "Project Folder", 160, delegate
		{
			using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
			{
				Description = "Unity 프로젝트 폴더 선택"
			};
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				txtProjectPath.Text = folderBrowserDialog.SelectedPath;
			}
		});
		txtOutputPath = CreatePathInput(leftPanel, "Output Folder", 235, delegate
		{
			using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
			{
				Description = "결과물 저장 폴더 선택"
			};
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				txtOutputPath.Text = folderBrowserDialog.SelectedPath;
			}
		});
		txtScreenshotPath = CreatePathInput(leftPanel, "스크린샷 저장 폴더", 310, delegate
		{
			using FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
			{
				Description = "스크린샷 저장 폴더 선택"
			};
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				txtScreenshotPath.Text = folderBrowserDialog.SelectedPath;
			}
		});
		txtDiscordWebhookUrl = CreateUrlInput(leftPanel, "일반 웹훅 URL", 385);
		txtAutoModeDiscordWebhookUrl = CreateUrlInput(leftPanel, "자동 모드 웹훅 URL", 460);
		Panel autoModePanel = new Panel
		{
			Location = new Point(24, 535),
			Size = new Size(232, 120)
		};
		btnToggleAutoMode = new RoundedButton
		{
			Text = "▶ 자동 모니터링 시작",
			Location = new Point(0, 0),
			Size = new Size(232, 38),
			ButtonStyle = RoundedButtonStyle.Secondary,
			BackColor = UiTheme.Secondary,
			CornerRadius = 6
		};
		btnToggleAutoMode.Click += btnToggleAutoMode_Click;
		Label intervalLabel = new Label
		{
			Text = "간격(분):",
			Location = new Point(0, 48),
			AutoSize = true,
			ForeColor = UiTheme.TextSecondary,
			Font = new Font("Segoe UI", 9f)
		};
		numInterval = new NumericUpDown
		{
			Location = new Point(70, 46),
			Width = 60,
			Value = 10m,
			Minimum = 1m,
			Maximum = 1440m,
			BackColor = UiTheme.CardBackground,
			ForeColor = UiTheme.TextPrimary,
			BorderStyle = BorderStyle.FixedSingle
		};
		lblStatusNotice = new Label
		{
			Text = "자동 모니터링 대기 중",
			Location = new Point(0, 80),
			Size = new Size(232, 40),
			ForeColor = UiTheme.TextMuted,
			Font = new Font("Segoe UI", 8.5f),
			TextAlign = ContentAlignment.TopLeft
		};
		autoModePanel.Controls.AddRange(btnToggleAutoMode, intervalLabel, numInterval, lblStatusNotice);
		leftPanel.Controls.Add(autoModePanel);
		RoundedButton saveButton = new RoundedButton
		{
			Text = "설정 저장",
			Dock = DockStyle.Bottom,
			Height = 42,
			ButtonStyle = RoundedButtonStyle.Secondary,
			Margin = new Padding(20, 0, 20, 20),
			CornerRadius = 6
		};
		saveButton.Click += delegate
		{
			SaveSettingsFromUI();
		};
		leftPanel.Controls.Add(saveButton);
		Panel centerPanel = new Panel
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(18, 22, 18, 18),
			BackColor = UiTheme.Background
		};
		Panel centerTopPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 44,
			Margin = new Padding(0, 0, 0, 15)
		};
		infoBadge = new NeonBadgePanel
		{
			Dock = DockStyle.Fill,
			Visible = true,
			BadgeText = "대기 중"
		};
		btnCopyReport = new RoundedButton
		{
			Text = "\ud83d\udccb 리포트 복사",
			Dock = DockStyle.Right,
			Width = 140,
			Height = 44,
			ButtonStyle = RoundedButtonStyle.Secondary,
			CornerRadius = 6
		};
		btnCopyReport.Click += delegate
		{
			if (!string.IsNullOrEmpty(reportTextBox.Text))
			{
				Clipboard.SetText(reportTextBox.Text);
				copyNotificationLabel.Visible = true;
				copyNotificationTimer.Start();
			}
		};
		centerTopPanel.Controls.Add(infoBadge);
		centerTopPanel.Controls.Add(btnCopyReport);
		automationProgressBar = new ModernProgressBar
		{
			Dock = DockStyle.Top,
			Height = 5,
			Value = 0,
			Margin = new Padding(0, 0, 0, 10)
		};
		Panel scoreboardPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 40,
			Margin = new Padding(0, 0, 0, 10)
		};
		lblTotalCount = CreateScoreboardLabel("TOTAL: 0", DockStyle.Left, UiTheme.TextSecondary);
		lblPassedCount = CreateScoreboardLabel("PASSED: 0", DockStyle.Left, Color.FromArgb(74, 222, 128));
		lblFailedCount = CreateScoreboardLabel("FAILED: 0", DockStyle.Left, UiTheme.Error);
		scoreboardPanel.Controls.AddRange(lblFailedCount, lblPassedCount, lblTotalCount);
		SplitContainer mainContentContainer = new SplitContainer
		{
			Dock = DockStyle.Fill,
			Orientation = Orientation.Horizontal,
			SplitterDistance = 310,
			BackColor = UiTheme.Background,
			SplitterWidth = 8
		};
		mainContentContainer.Panel1.BackColor = UiTheme.LogBackground;
		mainContentContainer.Panel2.BackColor = UiTheme.LogBackground;
		Panel reportContainer = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = UiTheme.LogBackground,
			Padding = new Padding(18)
		};
		reportTextBox = new DarkLogTextBox
		{
			Dock = DockStyle.Fill,
			Text = "준비 완료: 'QA 테스트 시작' 버튼을 누르면 검증이 진행됩니다.",
			ScrollBars = ScrollBars.None,
			WordWrap = true
		};
		copyNotificationLabel = new Label
		{
			Text = "✔ 리포트가 클립보드에 복사되었습니다!",
			Visible = false,
			BackColor = Color.FromArgb(50, UiTheme.Success),
			ForeColor = UiTheme.TextPrimary,
			TextAlign = ContentAlignment.MiddleCenter,
			Dock = DockStyle.Bottom,
			Height = 30
		};
		copyNotificationTimer = new System.Windows.Forms.Timer
		{
			Interval = 2000
		};
		copyNotificationTimer.Tick += delegate
		{
			copyNotificationLabel.Visible = false;
			copyNotificationTimer.Stop();
		};
		reportContainer.Controls.Add(copyNotificationLabel);
		reportContainer.Controls.Add(reportTextBox);
		mainContentContainer.Panel1.Controls.Add(reportContainer);
		picScreenshot = new PictureBox
		{
			Dock = DockStyle.Fill,
			SizeMode = PictureBoxSizeMode.Zoom,
			BackColor = Color.FromArgb(12, 14, 18),
			Visible = false
		};
		mainContentContainer.Panel2.Controls.Add(picScreenshot);
		centerPanel.Controls.Add(mainContentContainer);
		centerPanel.Controls.Add(scoreboardPanel);
		centerPanel.Controls.Add(automationProgressBar);
		centerPanel.Controls.Add(centerTopPanel);
		Panel rightPanel = new Panel
		{
			Dock = DockStyle.Right,
			Width = 320,
			Padding = new Padding(16, 22, 22, 18),
			BackColor = UiTheme.CardBackground
		};
		runTestButton = new RoundedButton
		{
			Text = "QA 테스트 시작",
			Dock = DockStyle.Top,
			Height = 44,
			Margin = new Padding(0, 0, 0, 15),
			CornerRadius = 6
		};
		runTestButton.Click += button1_Click;
		Panel logContainer = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = UiTheme.LogBackground,
			Height = 200,
			Padding = new Padding(14)
		};
		liveLogTextBox = new DarkLogTextBox
		{
			Dock = DockStyle.Fill,
			Text = "Unity 빌드 로그 대기 중...",
			ScrollBars = ScrollBars.None
		};
		logContainer.Controls.Add(liveLogTextBox);
		Label historyLabel = new Label
		{
			Text = "테스트 히스토리",
			Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
			ForeColor = Color.FromArgb(201, 209, 217),
			Dock = DockStyle.Top,
			Height = 30,
			Padding = new Padding(0, 10, 0, 0)
		};
		historyListBox = new ListBox
		{
			Dock = DockStyle.Bottom,
			Height = 220,
			BackColor = UiTheme.LogBackground,
			ForeColor = UiTheme.TextSecondary,
			BorderStyle = BorderStyle.None,
			Font = new Font("Consolas", 9f),
			DrawMode = DrawMode.OwnerDrawFixed,
			ItemHeight = 20
		};
		historyListBox.SelectedIndexChanged += historyListBox_SelectedIndexChanged;
		historyListBox.DrawItem += delegate(object? s, DrawItemEventArgs e)
		{
			e.DrawBackground();
			if (e.Index >= 0)
			{
				using SolidBrush brush = new SolidBrush(e.ForeColor);
				e.Graphics.DrawString(historyListBox.Items[e.Index].ToString(), e.Font, brush, e.Bounds, StringFormat.GenericDefault);
			}
			e.DrawFocusRectangle();
		};
		rightPanel.Controls.Add(logContainer);
		rightPanel.Controls.Add(historyLabel);
		rightPanel.Controls.Add(historyListBox);
		rightPanel.Controls.Add(runTestButton);
		base.Controls.Add(centerPanel);
		base.Controls.Add(rightPanel);
		base.Controls.Add(leftPanel);
		LoadSettingsToUI();
		LoadHistoryFiles();
		SetupLogFileWatcher();
		SetupRuntimeEvidenceWatcher();
		progressTimer = new System.Windows.Forms.Timer
		{
			Interval = 200
		};
		progressTimer.Tick += delegate
		{
			if (automationProgressBar.Value < 95)
			{
				automationProgressBar.Value += 2;
			}
		};
		autoModeTimer = new System.Windows.Forms.Timer();
		autoModeTimer.Tick += autoModeTimer_Tick;
	}

	private Label CreateScoreboardLabel(string text, DockStyle dock, Color color)
	{
		return new Label
		{
			Text = text,
			Dock = dock,
			Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
			ForeColor = color,
			TextAlign = ContentAlignment.MiddleCenter,
			Width = 112
		};
	}

	private TextBox CreatePathInput(Control parent, string labelText, int top, Action browseAction)
	{
		Label label = new Label
		{
			Text = labelText,
			Font = new Font("Segoe UI Semibold", 8.5f),
			ForeColor = UiTheme.TextSecondary,
			Location = new Point(24, top),
			AutoSize = true
		};
		TextBox textBox = new TextBox
		{
			Location = new Point(24, top + 24),
			Size = new Size(172, 27),
			BackColor = UiTheme.LogBackground,
			ForeColor = UiTheme.PathText,
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 9f)
		};
		Button browseButton = new Button
		{
			Text = "...",
			Location = new Point(textBox.Right + 5, textBox.Top),
			Size = new Size(36, textBox.Height),
			FlatStyle = FlatStyle.Flat,
			BackColor = UiTheme.Secondary,
			ForeColor = UiTheme.TextPrimary,
			Font = new Font("Segoe UI", 9f),
			Cursor = Cursors.Hand
		};
		browseButton.FlatAppearance.BorderSize = 0;
		browseButton.Click += delegate
		{
			browseAction();
		};
		parent.Controls.Add(label);
		parent.Controls.Add(textBox);
		parent.Controls.Add(browseButton);
		return textBox;
	}

	private TextBox CreateUrlInput(Control parent, string labelText, int top)
	{
		Label label = new Label
		{
			Text = labelText,
			Font = new Font("Segoe UI Semibold", 8.5f),
			ForeColor = UiTheme.TextSecondary,
			Location = new Point(24, top),
			AutoSize = true
		};
		TextBox textBox = new TextBox
		{
			Location = new Point(24, top + 24),
			Size = new Size(232, 27),
			BackColor = UiTheme.LogBackground,
			ForeColor = UiTheme.PathText,
			BorderStyle = BorderStyle.FixedSingle,
			Font = new Font("Segoe UI", 9f)
		};
		parent.Controls.AddRange(label, textBox);
		return textBox;
	}

}

