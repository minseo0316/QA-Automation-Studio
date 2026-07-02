using System;
using System.Drawing;
using System.Windows.Forms;
using MyWinFormsApp.Controls;

namespace MyWinFormsApp;

public class SettingsForm : Form
{
    private TextBox unityPathTextBox;
    private TextBox projectPathTextBox;
    private TextBox outputPathTextBox;

    public SettingsForm()
    {
        InitializeComponent();
        LoadSettingsToUI();
    }

    private void LoadSettingsToUI()
    {
        unityPathTextBox.Text = PathManager.UnityEditorPath;
        projectPathTextBox.Text = PathManager.UnityProjectPath;
        outputPathTextBox.Text = PathManager.OutputDirectory;
    }

    private void SaveSettingsFromUI()
    {
        PathManager.UnityEditorPath = unityPathTextBox.Text;
        PathManager.UnityProjectPath = projectPathTextBox.Text;
        PathManager.OutputDirectory = outputPathTextBox.Text;
        PathManager.SaveSettings();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        
        this.Text = "경로 설정";
        this.BackColor = UiTheme.Background;
        this.ForeColor = UiTheme.TextPrimary;
        this.Size = new Size(600, 320);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        // Unity Editor Path
        var unityLabel = new Label { Text = "Unity 에디터 경로", Location = new Point(20, 25), Size = new Size(150, 20), ForeColor = UiTheme.TextSecondary };
        unityPathTextBox = new TextBox { Location = new Point(20, 50), Size = new Size(440, 23), BackColor = UiTheme.CardBackground, ForeColor = UiTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
        var browseUnityButton = new Button { Text = "찾아보기", Location = new Point(470, 49), Size = new Size(90, 25) };
        browseUnityButton.Click += (s, e) =>
        {
            using var dialog = new OpenFileDialog { Filter = "Unity Editor|Unity.exe", Title = "Unity.exe를 선택하세요" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                unityPathTextBox.Text = dialog.FileName;
            }
        };

        // Unity Project Path
        var projectLabel = new Label { Text = "Unity 프로젝트 폴더", Location = new Point(20, 95), Size = new Size(150, 20), ForeColor = UiTheme.TextSecondary };
        projectPathTextBox = new TextBox { Location = new Point(20, 120), Size = new Size(440, 23), BackColor = UiTheme.CardBackground, ForeColor = UiTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
        var browseProjectButton = new Button { Text = "찾아보기", Location = new Point(470, 119), Size = new Size(90, 25) };
        browseProjectButton.Click += (s, e) =>
        {
            using var dialog = new FolderBrowserDialog { Description = "Unity 프로젝트 폴더를 선택하세요" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                projectPathTextBox.Text = dialog.SelectedPath;
            }
        };

        // Output Directory
        var outputLabel = new Label { Text = "결과물 저장 폴더", Location = new Point(20, 165), Size = new Size(150, 20), ForeColor = UiTheme.TextSecondary };
        outputPathTextBox = new TextBox { Location = new Point(20, 190), Size = new Size(440, 23), BackColor = UiTheme.CardBackground, ForeColor = UiTheme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
        var browseOutputButton = new Button { Text = "찾아보기", Location = new Point(470, 189), Size = new Size(90, 25) };
        browseOutputButton.Click += (s, e) =>
        {
            using var dialog = new FolderBrowserDialog { Description = "테스트 결과물이 저장될 폴더를 선택하세요" };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                outputPathTextBox.Text = dialog.SelectedPath;
            }
        };

        // Save and Cancel Buttons
        var saveButton = new RoundedButton
        {
            Text = "저장",
            Location = new Point(350, 240),
            Size = new Size(100, 35),
            DialogResult = DialogResult.OK
        };
        saveButton.Click += (s, e) => SaveSettingsFromUI();

        var cancelButton = new RoundedButton
        {
            Text = "취소",
            Location = new Point(470, 240),
            Size = new Size(100, 35),
            ButtonStyle = RoundedButtonStyle.Secondary,
            DialogResult = DialogResult.Cancel
        };

        this.Controls.Add(unityLabel);
        this.Controls.Add(unityPathTextBox);
        this.Controls.Add(browseUnityButton);
        this.Controls.Add(projectLabel);
        this.Controls.Add(projectPathTextBox);
        this.Controls.Add(browseProjectButton);
        this.Controls.Add(outputLabel);
        this.Controls.Add(outputPathTextBox);
        this.Controls.Add(browseOutputButton);
        this.Controls.Add(saveButton);
        this.Controls.Add(cancelButton);

        this.AcceptButton = saveButton;
        this.CancelButton = cancelButton;

        this.ResumeLayout(false);
    }
}