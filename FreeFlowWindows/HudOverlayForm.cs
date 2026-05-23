using System.Drawing.Drawing2D;

namespace FreeFlowWindows;

internal sealed class HudOverlayForm : Form
{
    private readonly System.Windows.Forms.Timer animationTimer = new();
    private readonly System.Windows.Forms.Timer hideTimer = new();
    private readonly Random random = new(7);

    private DictationStatus status = DictationStatus.Info;
    private string message = "";
    private float phase;
    private float[] bars = { 0.35f, 0.7f, 1.0f, 0.65f, 0.4f };

    public HudOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Width = 420;
        Height = 96;
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;
        DoubleBuffered = true;
        Opacity = 0.96;

        animationTimer.Interval = 33;
        animationTimer.Tick += (_, _) =>
        {
            phase += 0.08f;
            if (status == DictationStatus.Listening)
            {
                for (var i = 0; i < bars.Length; i++)
                {
                    var wave = (float)((Math.Sin(phase * 2.5f + i * 0.9f) + 1) * 0.5);
                    var jitter = (float)(random.NextDouble() * 0.08);
                    bars[i] = Math.Clamp(0.28f + wave * 0.72f + jitter, 0.18f, 1.0f);
                }
            }
            Invalidate();
        };

        hideTimer.Tick += (_, _) =>
        {
            hideTimer.Stop();
            Hide();
            animationTimer.Stop();
        };
    }

    public void ShowStatus(DictationStatus newStatus, string newMessage)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => ShowStatus(newStatus, newMessage));
            return;
        }

        status = newStatus;
        message = newMessage;
        hideTimer.Stop();

        Size = WindowSizeFor(newStatus);
        PositionTopCenter();
        Show();
        BringToFront();

        if (!animationTimer.Enabled)
        {
            animationTimer.Start();
        }

        if (newStatus is DictationStatus.Success)
        {
            hideTimer.Interval = 650;
            hideTimer.Start();
        }
        else if (newStatus is DictationStatus.Error or DictationStatus.Info)
        {
            hideTimer.Interval = 3000;
            hideTimer.Start();
        }
    }

    public void HideStatus()
    {
        if (InvokeRequired)
        {
            BeginInvoke(HideStatus);
            return;
        }

        hideTimer.Stop();
        Hide();
        animationTimer.Stop();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.Clear(TransparencyKey);

        var pill = PillRectFor(status);
        DrawPill(g, pill);

        switch (status)
        {
            case DictationStatus.Listening:
                DrawWaveform(g, pill);
                break;
            case DictationStatus.Processing:
                DrawBreathingBar(g, pill);
                break;
            case DictationStatus.Success:
                DrawCheckmark(g, pill);
                break;
            case DictationStatus.Error:
                DrawText(g, pill, Shorten(message), Color.FromArgb(255, 205, 205));
                break;
            case DictationStatus.Info:
                DrawText(g, pill, Shorten(message), Color.FromArgb(235, 238, 255));
                break;
        }
    }

    private void DrawPill(Graphics g, RectangleF pill)
    {
        using var path = RoundedPath(pill, pill.Height / 2);
        using var fill = new SolidBrush(Color.FromArgb(118, 10, 10, 14));
        using var border = new Pen(Color.FromArgb(178, 255, 255, 255), 1.8f);
        g.FillPath(fill, path);
        g.DrawPath(border, path);

        if (status == DictationStatus.Processing)
        {
            var pulse = (float)((Math.Sin(phase * 2.0f) + 1) * 0.5);
            using var glow = new Pen(Color.FromArgb((int)(45 + pulse * 80), 210, 230, 255), 4);
            g.DrawPath(glow, path);
        }
    }

    private void DrawWaveform(Graphics g, RectangleF pill)
    {
        var centerX = pill.Left + pill.Width / 2;
        var centerY = pill.Top + pill.Height / 2;
        var barWidth = 4f;
        var gap = 5f;
        var maxHeight = 20f;
        var totalWidth = bars.Length * barWidth + (bars.Length - 1) * gap;
        var startX = centerX - totalWidth / 2;

        using var brush = new SolidBrush(Color.FromArgb(235, 255, 255, 255));
        for (var i = 0; i < bars.Length; i++)
        {
            var h = 5f + bars[i] * maxHeight;
            var x = startX + i * (barWidth + gap);
            var y = centerY - h / 2;
            using var path = RoundedPath(new RectangleF(x, y, barWidth, h), barWidth / 2);
            g.FillPath(brush, path);
        }
    }

    private void DrawBreathingBar(Graphics g, RectangleF pill)
    {
        var pulse = (float)((Math.Sin(phase * 2.2f) + 1) * 0.5);
        var width = 18f + pulse * 24f;
        var rect = new RectangleF(
            pill.Left + (pill.Width - width) / 2,
            pill.Top + (pill.Height - 4) / 2,
            width,
            4);
        using var brush = new SolidBrush(Color.FromArgb((int)(120 + pulse * 115), 255, 255, 255));
        using var path = RoundedPath(rect, 2);
        g.FillPath(brush, path);
    }

    private static void DrawText(Graphics g, RectangleF pill, string text, Color color)
    {
        using var font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        g.DrawString(text, font, brush, pill, format);
    }

    private static void DrawCheckmark(Graphics g, RectangleF pill)
    {
        var cx = pill.Left + pill.Width / 2;
        var cy = pill.Top + pill.Height / 2;
        var points = new[]
        {
            new PointF(cx - 9, cy),
            new PointF(cx - 3, cy + 6),
            new PointF(cx + 10, cy - 7)
        };
        using var pen = new Pen(Color.FromArgb(230, 225, 255, 235), 2.9f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        g.DrawLines(pen, points);
    }

    private RectangleF PillRectFor(DictationStatus state)
    {
        var size = state switch
        {
            DictationStatus.Listening => new SizeF(84, 32),
            DictationStatus.Processing => new SizeF(48, 10),
            DictationStatus.Success => new SizeF(58, 28),
            DictationStatus.Error => new SizeF(260, 32),
            DictationStatus.Info => new SizeF(190, 30),
            _ => new SizeF(80, 28)
        };
        return new RectangleF(
            (ClientSize.Width - size.Width) / 2,
            20,
            size.Width,
            size.Height);
    }

    private Size WindowSizeFor(DictationStatus state) => state switch
    {
        DictationStatus.Error => new Size(340, 76),
        DictationStatus.Info => new Size(260, 72),
        _ => new Size(180, 72)
    };

    private void PositionTopCenter()
    {
        var cursor = Cursor.Position;
        var screen = Screen.FromPoint(cursor).WorkingArea;
        Location = new Point(
            screen.Left + (screen.Width - Width) / 2,
            screen.Top + 18);
    }

    private static GraphicsPath RoundedPath(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static string Shorten(string value)
    {
        const int max = 42;
        return value.Length <= max ? value : value[..(max - 1)] + "...";
    }
}
