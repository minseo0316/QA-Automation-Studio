namespace MyWinFormsApp;

/// <summary>
/// Stores local QA runner settings in config.json.
/// </summary>
public class AppSettings
{
    public string UnityEditorPath { get; set; } = string.Empty;
    public string UnityProjectPath { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = Path.Combine(Application.StartupPath, "TestResults");
    public string DiscordWebhookUrl { get; set; } = string.Empty;
    public string AutoModeDiscordWebhookUrl { get; set; } = string.Empty;
    public string ScreenshotDirectory { get; set; } = Path.Combine(Application.StartupPath, "TestResults", "History");
}
