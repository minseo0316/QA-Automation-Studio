using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyWinFormsApp.Controls;

internal class ModernProgressBar : Control
{
    private int _value;
    private int _animatedValue;
    private readonly System.Windows.Forms.Timer _animationTimer;

    public ModernProgressBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        Height = 8;
        Minimum = 0;
        Maximum = 100;
        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animationTimer.Tick += AnimationTimer_Tick;
    }

    [DefaultValue(0)]
    public int Minimum { get; set; }

    [DefaultValue(100)]
    public int Maximum { get; set; } = 100;

    [DefaultValue(0)]
    public int Value
    {
        get => _value;
        set
        {
            int clamped = Math.Clamp(value, Minimum, Maximum);
            if (_value == clamped)
            {
                return;
            }

            _value = clamped;
            if (_animatedValue != _value && !_animationTimer.Enabled)
            {
                _animationTimer.Start();
            }
        }
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        int diff = _value - _animatedValue;
        if (Math.Abs(diff) <= 1)
        {
            _animatedValue = _value;
            _animationTimer.Stop();
        }
        else
        {
            _animatedValue += (int)Math.Sign(diff) * Math.Max(1, Math.Abs(diff) / 6);
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? UiTheme.CardBackground);

        var trackRect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var trackPath = CreateRoundedPath(trackRect, Height / 2);
        using var trackBrush = new SolidBrush(UiTheme.ProgressTrack);
        e.Graphics.FillPath(trackBrush, trackPath);

        if (_animatedValue <= Minimum)
        {
            return;
        }

        float ratio = (float)(_animatedValue - Minimum) / Math.Max(1, Maximum - Minimum);
        int fillWidth = Math.Max(Height, (int)(Width * ratio));
        var fillRect = new Rectangle(0, 0, fillWidth, Height - 1);
        using var fillPath = CreateRoundedPath(fillRect, Height / 2);
        using var fillBrush = new LinearGradientBrush(
            fillRect,
            UiTheme.Primary,
            UiTheme.PrimaryHover,
            LinearGradientMode.Horizontal);
        e.Graphics.FillPath(fillBrush, fillPath);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int diameter = Math.Max(2, radius * 2);
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
