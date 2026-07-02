using System;
using System.ComponentModel; // 💡 [오류 해결 핵심] 디자이너 속성 제어 도구 상자
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyWinFormsApp.Controls;

// 💡 뱃지 스타일을 명확하게 구분하기 위한 열거형 정의
internal enum BadgeStyle
{
    Info,
    Success,
    Error
}

internal class NeonBadgePanel : Panel
{
    private string _badgeText = "";
    private BadgeStyle _style = BadgeStyle.Info;

    public NeonBadgePanel()
    {
        Height = 42;
        Font = UiTheme.SuccessBadgeFont;
        Padding = new Padding(14, 8, 14, 8);
        Visible = false;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
    }

    [Browsable(true)]
    [Category("Appearance")]
    [Description("뱃지 내부에 표시할 한국어 알림 문구를 지정합니다.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string BadgeText
    {
        get => _badgeText;
        set
        {
            _badgeText = value;
            UpdateStyleFromText();
            Invalidate();
        }
    }
    
    // 💡 [오류 해결 완료 핵심 영역] 디자이너가 이 열거형 프로퍼티를 직렬화 코드 생성에서 안전하게 다루도록 강제 지시합니다.
    [Browsable(true)]
    [Category("Appearance")]
    [Description("뱃지의 시각적 스타일(Info, Success, Error)을 지정합니다.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)] // 👈 WFO1000 빨간 줄을 파괴하는 라인
    public BadgeStyle Style
    {
        get => _style;
        set
        {
            _style = value;
            Invalidate();
        }
    }

    private void UpdateStyleFromText()
    {
        if (_badgeText.Contains("✔") || _badgeText.Contains("SUCCESS"))
        {
            _style = BadgeStyle.Success;
        }
        else if (_badgeText.Contains("❌") || _badgeText.Contains("결함"))
        {
            _style = BadgeStyle.Error;
        }
        else
        {
            _style = BadgeStyle.Info;
        }
    }
    
    protected override void OnPaint(PaintEventArgs e)
    {
        Color backgroundColor, borderColor, glowColor, textColor;
        switch (_style)
        {
            case BadgeStyle.Success:
                backgroundColor = UiTheme.SuccessBackground;
                borderColor = Color.FromArgb(80, UiTheme.Success);
                glowColor = Color.FromArgb(24, UiTheme.Success);
                textColor = UiTheme.Success;
                break;
            case BadgeStyle.Error:
                backgroundColor = UiTheme.ErrorBackground;
                borderColor = Color.FromArgb(80, UiTheme.Error);
                glowColor = Color.FromArgb(24, UiTheme.Error);
                textColor = UiTheme.Error;
                break;
            default: // Info
                backgroundColor = UiTheme.CardBackground;
                borderColor = Color.FromArgb(80, UiTheme.Primary);
                glowColor = Color.FromArgb(18, UiTheme.Primary);
                textColor = UiTheme.TextSecondary;
                break;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? UiTheme.Background);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CreateRoundedPath(bounds, 6);
        using var fillBrush = new SolidBrush(backgroundColor);
        using var borderPen = new Pen(borderColor);
        e.Graphics.FillPath(fillBrush, path);
        e.Graphics.DrawPath(borderPen, path);

        using var glowBrush = new SolidBrush(glowColor);
        e.Graphics.FillEllipse(glowBrush, 8, 8, 12, 12);

        TextRenderer.DrawText(
            e.Graphics,
            _badgeText,
            Font,
            new Rectangle(28, 0, Width - 32, Height),
            textColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
