using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MyWinFormsApp.Controls;

internal enum RoundedButtonStyle
{
    Primary,
    Secondary
}

internal class RoundedButton : Button
{
    private bool _isHovered;
    private bool _isPressed;
    private int _cornerRadius = 6;
    private Color _currentFillColor = UiTheme.Primary;
    private readonly System.Windows.Forms.Timer _transitionTimer;
    private float _transitionProgress;
    private Color _transitionFrom;
    private Color _transitionTo;

    public RoundedButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = UiTheme.Primary;
        ForeColor = Color.White;
        Font = UiTheme.ButtonFont;
        Cursor = Cursors.Hand;
        Size = new Size(260, 44);
        ButtonStyle = RoundedButtonStyle.Primary;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        _transitionTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _transitionTimer.Tick += TransitionTimer_Tick;
    }

    [DefaultValue(RoundedButtonStyle.Primary)]
    public RoundedButtonStyle ButtonStyle { get; set; }

    [Browsable(true)]
    [Category("Design")]
    [Description("버튼 모서리의 반지름 크기를 지정합니다.")]
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
        AnimateTo(GetTargetColor());
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isHovered = false;
        _isPressed = false;
        AnimateTo(GetBaseColor());
    }

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        if (mevent.Button == MouseButtons.Left)
        {
            _isPressed = true;
            AnimateTo(GetPressedColor());
        }
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isPressed = false;
        AnimateTo(GetTargetColor());
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        _currentFillColor = Enabled ? GetBaseColor() : UiTheme.Secondary;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        pevent.Graphics.Clear(Parent?.BackColor ?? UiTheme.CardBackground);

        int currentRadius = _cornerRadius;
        if (currentRadius * 2 > Width || currentRadius * 2 > Height)
        {
            currentRadius = Math.Min(Width, Height) / 2;
        }

        if (currentRadius < 1)
        {
            currentRadius = 1;
        }

        using var path = CreateRoundedPath(ClientRectangle, currentRadius);
        using var brush = new SolidBrush(_currentFillColor);
        pevent.Graphics.FillPath(brush, path);

        if (ButtonStyle == RoundedButtonStyle.Secondary && _isHovered && Enabled)
        {
            using var borderPen = new Pen(Color.FromArgb(120, UiTheme.Primary));
            pevent.Graphics.DrawPath(borderPen, path);
        }

        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            Font,
            ClientRectangle,
            Enabled ? ForeColor : UiTheme.TextMuted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    public void ResetVisualState()
    {
        _isHovered = false;
        _isPressed = false;
        _currentFillColor = Enabled ? GetBaseColor() : UiTheme.Secondary;
        Invalidate();
    }

    private void TransitionTimer_Tick(object? sender, EventArgs e)
    {
        _transitionProgress += 0.18f;
        _currentFillColor = ColorHelper.Lerp(_transitionFrom, _transitionTo, Math.Min(_transitionProgress, 1f));

        if (_transitionProgress >= 1f)
        {
            _currentFillColor = _transitionTo;
            _transitionTimer.Stop();
        }

        Invalidate();
    }

    public void AnimateTo(Color target)
    {
        _transitionFrom = _currentFillColor;
        _transitionTo = target;
        _transitionProgress = 0f;
        _transitionTimer.Start();
    }

    private Color GetBaseColor()
    {
        return ButtonStyle == RoundedButtonStyle.Secondary ? UiTheme.Secondary : UiTheme.Primary;
    }

    private Color GetHoverColor()
    {
        return ButtonStyle == RoundedButtonStyle.Secondary ? UiTheme.SecondaryHover : UiTheme.PrimaryHover;
    }

    private Color GetPressedColor()
    {
        return ButtonStyle == RoundedButtonStyle.Secondary ? UiTheme.SecondaryPressed : UiTheme.PrimaryPressed;
    }

    private Color GetTargetColor()
    {
        if (!Enabled)
        {
            return UiTheme.Secondary;
        }

        return _isHovered ? GetHoverColor() : GetBaseColor();
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
