using System.Drawing;

namespace MyWinFormsApp;

internal static class UiTheme
{
    // 💡 [디자인 개편] 프리미엄 데이터 대시보드 컬러 스킴 적용
    public static readonly Color Background = Color.FromArgb(15, 17, 21);
    public static readonly Color CardBackground = Color.FromArgb(24, 27, 32);
    public static readonly Color CardBorder = Color.FromArgb(45, 49, 57);
    public static readonly Color CardBorderHover = Color.FromArgb(66, 72, 82);
    public static readonly Color TextPrimary = Color.FromArgb(242, 244, 248);
    public static readonly Color TextSecondary = Color.FromArgb(174, 180, 190);
    public static readonly Color TextMuted = Color.FromArgb(120, 127, 139);
    public static readonly Color PathText = Color.FromArgb(205, 210, 218);
    public static readonly Color Primary = Color.FromArgb(45, 133, 240);
    public static readonly Color PrimaryHover = Color.FromArgb(67, 148, 246);
    public static readonly Color PrimaryPressed = Color.FromArgb(32, 108, 204);
    public static readonly Color Secondary = Color.FromArgb(37, 41, 48);
    public static readonly Color SecondaryHover = Color.FromArgb(49, 54, 63);
    public static readonly Color SecondaryPressed = Color.FromArgb(58, 64, 74);
    public static readonly Color Success = Color.FromArgb(48, 190, 112);
    public static readonly Color SuccessGlow = Color.FromArgb(40, 35, 134, 54);
    public static readonly Color SuccessBackground = Color.FromArgb(22, 36, 28);
    public static readonly Color Error = Color.FromArgb(248, 113, 113);
    public static readonly Color ErrorBackground = Color.FromArgb(45, 30, 30);
    public static readonly Color Warning = Color.FromArgb(251, 191, 36);
    public static readonly Color LogBackground = Color.FromArgb(19, 22, 27);
    public static readonly Color LogBorder = Color.FromArgb(45, 49, 57);
    public static readonly Color FooterBackground = Color.FromArgb(15, 17, 21);
    public static readonly Color ProgressTrack = Color.FromArgb(42, 46, 54);
    public static readonly Color ProgressFill = Color.FromArgb(0, 163, 255);       // #00A3FF

    public static Font TitleFont => new("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font SubtitleFont => new("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font BodyFont => new("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font ButtonFont => new("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font StatusFont => new("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
    public static Font PathFont => new("Consolas", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font LogFont => new("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
    public static Font SuccessBadgeFont => new("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
}
