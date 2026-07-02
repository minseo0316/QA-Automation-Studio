using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace MyWinFormsApp;

internal static class PathManager
{
    private const string ConfigFileName = "config.json";
    private static readonly string ConfigFilePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
    private static AppSettings _settings = new AppSettings();

    static PathManager()
    {
        LoadSettings();
    }

    public static string UnityEditorPath { get => _settings.UnityEditorPath; set => _settings.UnityEditorPath = value; }
    public static string UnityProjectPath { get => _settings.UnityProjectPath; set => _settings.UnityProjectPath = value; }
    public static string OutputDirectory { get => _settings.OutputDirectory; set => _settings.OutputDirectory = value; }
    public static string DiscordWebhookUrl { get => _settings.DiscordWebhookUrl; set => _settings.DiscordWebhookUrl = value; }
    public static string AutoModeDiscordWebhookUrl { get => _settings.AutoModeDiscordWebhookUrl; set => _settings.AutoModeDiscordWebhookUrl = value; }
    public static string ScreenshotDirectory { get => _settings.ScreenshotDirectory; set => _settings.ScreenshotDirectory = value; }

    public static string XmlReportPath => Path.Combine(OutputDirectory, "Result.xml");
    public static string UnityLogPath => Path.Combine(OutputDirectory, "Unity_QA_Log.txt");
    public static string FinalReportPath => Path.Combine(OutputDirectory, "QA_Final_Report.txt");

    private static void LoadSettings()
    {
        if (File.Exists(ConfigFilePath))
        {
            string json = File.ReadAllText(ConfigFilePath);
            _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        else
        {
            _settings = new AppSettings();
        }
    }

    public static void SaveSettings()
    {
        string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFilePath, json);
    }
}
