using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyWinFormsApp.Controls;

internal class CardPanel : Panel
{
    private int _cornerRadius = 14;
    private bool _isHovered;
    private Color _currentBorderColor = UiTheme.CardBorder;
    private readonly System.Windows.Forms.Timer _transitionTimer;
    private float _transitionProgress;
    private Color _transitionFrom;
    private Color _transitionTo;

    public CardPanel()
    {
        BackColor = UiTheme.CardBackground;
        Padding = new Padding(28, 24, 28, 24);
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        _transitionTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _transitionTimer.Tick += TransitionTimer_Tick;
    }

    [Browsable(true)]
    [Category("Design")]
    [Description("카드의 라운드 모서리 반지름 크기를 지정합니다.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set
        {
            int maxRadius = Math.Min(Width, Height) / 2;
            _cornerRadius = Math.Clamp(value, 1, Math.Max(1, maxRadius));
            Invalidate();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isHovered = true;
        AnimateBorderTo(UiTheme.CardBorderHover);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        AnimateBorderTo(UiTheme.CardBorder);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(Parent?.BackColor ?? UiTheme.Background);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        int currentRadius = _cornerRadius;
        if (currentRadius * 2 > Width || currentRadius * 2 > Height)
        {
            currentRadius = Math.Min(Width, Height) / 2;
        }

        if (currentRadius < 1)
        {
            currentRadius = 1;
        }

        using var path = CreateRoundedPath(bounds, currentRadius);
        using var fillBrush = new SolidBrush(BackColor);
        using var borderPen = new Pen(_currentBorderColor);

        e.Graphics.FillPath(fillBrush, path);
        e.Graphics.DrawPath(borderPen, path);

        if (_isHovered)
        {
            using var glowPen = new Pen(Color.FromArgb(30, UiTheme.Primary));
            e.Graphics.DrawPath(glowPen, path);
        }
    }

    private void TransitionTimer_Tick(object? sender, EventArgs e)
    {
        _transitionProgress += 0.16f;
        _currentBorderColor = ColorHelper.Lerp(_transitionFrom, _transitionTo, Math.Min(_transitionProgress, 1f));

        if (_transitionProgress >= 1f)
        {
            _currentBorderColor = _transitionTo;
            _transitionTimer.Stop();
        }

        Invalidate();
    }

    private void AnimateBorderTo(Color target)
    {
        _transitionFrom = _currentBorderColor;
        _transitionTo = target;
        _transitionProgress = 0f;
        _transitionTimer.Start();
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _transitionTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
