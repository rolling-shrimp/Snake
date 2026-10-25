using System.Drawing.Drawing2D;

namespace Snake.WinForms;

public class GameProgressControl : Control
{
    private int _progressPercent;

    public GameProgressControl()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        Height = 24;
        AccessibleRole = AccessibleRole.ProgressBar;
        AccessibleName = "過關進度";
    }

    public void SetProgress(int progressPercent)
    {
        _progressPercent = Math.Clamp(progressPercent, 0, 100);
        AccessibleDescription = $"過關進度 {_progressPercent}%";
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        Graphics graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        RectangleF bounds = new RectangleF(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
        using GraphicsPath trackPath = CreateRoundedRectangle(bounds, bounds.Height / 2f);
        using SolidBrush trackBrush = new SolidBrush(Color.FromArgb(215, 221, 225));
        using Pen trackPen = new Pen(Color.FromArgb(158, 171, 180), 2);
        graphics.FillPath(trackBrush, trackPath);
        graphics.DrawPath(trackPen, trackPath);

        if (_progressPercent == 0)
        {
            return;
        }

        float fillWidth = Math.Max(bounds.Height, bounds.Width * _progressPercent / 100f);
        RectangleF fillBounds = new RectangleF(bounds.X, bounds.Y, fillWidth, bounds.Height);
        using GraphicsPath fillPath = CreateRoundedRectangle(fillBounds, fillBounds.Height / 2f);
        using LinearGradientBrush fillBrush = new LinearGradientBrush(
            fillBounds,
            Color.FromArgb(126, 233, 79),
            Color.FromArgb(40, 183, 71),
            90f);
        graphics.FillPath(fillBrush, fillPath);
    }

    private GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
    {
        float diameter = radius * 2;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
