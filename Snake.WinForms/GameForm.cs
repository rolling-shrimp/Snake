using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Snake;

public class GameForm : Form
{
    private const int LOGICAL_WIDTH = 1536;
    private const int LOGICAL_HEIGHT = 1024;
    private const int BOARD_X = 70;
    private const int BOARD_Y = 112;
    private const int BOARD_SIZE = 760;
    private const int CELL_SIZE = 76;
    private const int SIDEBAR_X = 900;
    private const int SIDEBAR_Y = 104;
    private const int SIDEBAR_WIDTH = 566;
    private const int SIDEBAR_HEIGHT = 790;

    private SnakeGame _game;
    private System.Windows.Forms.Timer _frameTimer = new System.Windows.Forms.Timer();
    private List<string> _renderedText = new List<string>();
    private Rectangle _startButton = new Rectangle(1062, 790, 242, 70);
    private Rectangle _replayButton = new Rectangle(1062, 790, 242, 70);
    private float _renderScale = 1.0f;
    private float _renderOffsetX;
    private float _renderOffsetY;
    private bool _faulted;

    public GameForm() : this(new SnakeGame(), true)
    {
    }

    internal GameForm(SnakeGame game, bool initializeFirstLevel = false)
    {
        _game = game;
        Text = string.Empty;
        ClientSize = new Size(LOGICAL_WIDTH, LOGICAL_HEIGHT);
        MinimumSize = new Size(960, 640);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = true;
        KeyPreview = true;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(245, 244, 236);

        _frameTimer.Interval = 16;
        _frameTimer.Tick += OnFrameTimerTick;
        KeyDown += OnGameKeyDown;
        MouseDown += OnGameMouseDown;
        FormClosed += OnGameFormClosed;

        if (initializeFirstLevel)
        {
            ExecuteUiOperation(nameof(SnakeGame.ShowFirstLevelReadyScreen), () =>
            {
                _game.ShowFirstLevelReadyScreen();
                _game.UpdateLevelStatus(1);
            });
        }

        _frameTimer.Start();
    }

    internal IReadOnlyList<string> RenderedText => _renderedText;
    internal SnakeGame Game => _game;

    internal void StopFrameTimerForTest()
    {
        _frameTimer.Stop();
    }

    internal void ClickAtForTest(Point position)
    {
        OnGameMouseDown(this, new MouseEventArgs(MouseButtons.Left, 1, position.X, position.Y, 0));
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        _renderedText.Clear();
        Graphics graphics = eventArgs.Graphics;
        GraphicsState savedState = graphics.Save();
        try
        {
            _renderScale = Math.Min((float)ClientSize.Width / LOGICAL_WIDTH, (float)ClientSize.Height / LOGICAL_HEIGHT);
            _renderOffsetX = (ClientSize.Width - LOGICAL_WIDTH * _renderScale) / 2.0f;
            _renderOffsetY = (ClientSize.Height - LOGICAL_HEIGHT * _renderScale) / 2.0f;
            graphics.TranslateTransform(_renderOffsetX, _renderOffsetY);
            graphics.ScaleTransform(_renderScale, _renderScale);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            VerifyVisibleContract();
            DrawScene(graphics);
        }
        catch (Exception exception)
        {
            HandleUnrecognizedFailure("畫面繪製", exception);
        }
        finally
        {
            graphics.Restore(savedState);
        }
    }

    private void DrawScene(Graphics graphics)
    {
        using (SolidBrush backgroundBrush = new SolidBrush(Color.FromArgb(245, 244, 236)))
        {
            graphics.FillRectangle(backgroundBrush, 0, 0, LOGICAL_WIDTH, LOGICAL_HEIGHT);
        }

        DrawBoard(graphics);
        DrawSidebar(graphics);
    }

    private void DrawBoard(Graphics graphics)
    {
        using (SolidBrush boardBrush = new SolidBrush(Color.FromArgb(255, 253, 244)))
        using (Pen boardBorder = new Pen(Color.FromArgb(88, 120, 102), 5))
        using (Pen gridPen = new Pen(Color.FromArgb(174, 197, 178), 2))
        {
            graphics.FillRectangle(boardBrush, BOARD_X, BOARD_Y, BOARD_SIZE, BOARD_SIZE);

            for (int row = 0; row < 10; row++)
            {
                for (int column = 0; column < 10; column++)
                {
                    if ((row + column) % 2 == 1)
                    {
                        using SolidBrush tileBrush = new SolidBrush(Color.FromArgb(234, 243, 229));
                        graphics.FillRectangle(tileBrush, BOARD_X + column * CELL_SIZE, BOARD_Y + row * CELL_SIZE, CELL_SIZE, CELL_SIZE);
                    }
                }
            }

            for (int index = 0; index <= 10; index++)
            {
                graphics.DrawLine(gridPen, BOARD_X + index * CELL_SIZE, BOARD_Y, BOARD_X + index * CELL_SIZE, BOARD_Y + BOARD_SIZE);
                graphics.DrawLine(gridPen, BOARD_X, BOARD_Y + index * CELL_SIZE, BOARD_X + BOARD_SIZE, BOARD_Y + index * CELL_SIZE);
            }

            graphics.DrawRectangle(boardBorder, BOARD_X, BOARD_Y, BOARD_SIZE, BOARD_SIZE);
        }

        foreach (Point wall in _game.Walls)
        {
            DrawWall(graphics, wall);
        }

        for (int doorIndex = 0; doorIndex < _game.Doors.Count; doorIndex++)
        {
            DrawDoor(graphics, doorIndex);
        }

        foreach (KeyValuePair<Point, GameObjectType> gameObject in _game.Objects)
        {
            DrawObject(graphics, gameObject.Key, gameObject.Value);
        }

        DrawSnake(graphics);
    }

    private void DrawWall(Graphics graphics, Point position)
    {
        Rectangle rectangle = GetCellRectangle(position, 4);
        using (SolidBrush wallBrush = new SolidBrush(Color.FromArgb(80, 96, 87)))
        using (Pen wallPen = new Pen(Color.FromArgb(55, 70, 61), 3))
        {
            graphics.FillRectangle(wallBrush, rectangle);
            graphics.DrawRectangle(wallPen, rectangle);
        }

        using Pen seamPen = new Pen(Color.FromArgb(136, 150, 138), 2);
        graphics.DrawLine(seamPen, rectangle.Left + 8, rectangle.Top + 18, rectangle.Right - 8, rectangle.Top + 18);
        graphics.DrawLine(seamPen, rectangle.Left + 8, rectangle.Bottom - 18, rectangle.Right - 8, rectangle.Bottom - 18);
    }

    private void DrawDoor(Graphics graphics, int doorIndex)
    {
        Point position = _game.Doors[doorIndex];
        Rectangle rectangle = GetCellRectangle(position, 8);
        bool isOpen = _game.IsDoorOpen(doorIndex);
        bool isWarning = _game.IsDoorWarning(doorIndex);
        bool warningVisible = !isWarning || Environment.TickCount64 / 250 % 2 == 0;
        Color fillColor = isOpen ? Color.FromArgb(180, 218, 166) : Color.FromArgb(177, 91, 70);
        Color edgeColor = isOpen ? Color.FromArgb(73, 125, 65) : Color.FromArgb(111, 52, 43);
        if (!warningVisible)
        {
            fillColor = Color.FromArgb(248, 223, 145);
            edgeColor = Color.FromArgb(210, 142, 47);
        }

        using (SolidBrush doorBrush = new SolidBrush(fillColor))
        using (Pen doorPen = new Pen(edgeColor, 4))
        {
            graphics.FillRectangle(doorBrush, rectangle);
            graphics.DrawRectangle(doorPen, rectangle);
        }

        if (!isOpen)
        {
            using Pen barPen = new Pen(Color.FromArgb(255, 240, 224), 4);
            graphics.DrawLine(barPen, rectangle.Left + 10, rectangle.Top + 12, rectangle.Right - 10, rectangle.Bottom - 12);
            graphics.DrawLine(barPen, rectangle.Right - 10, rectangle.Top + 12, rectangle.Left + 10, rectangle.Bottom - 12);
        }
        else
        {
            using Pen openingPen = new Pen(Color.FromArgb(245, 255, 228), 4);
            graphics.DrawLine(openingPen, rectangle.Left + 12, rectangle.Bottom - 12, rectangle.Right - 12, rectangle.Bottom - 12);
        }
    }

    private void DrawObject(Graphics graphics, Point position, GameObjectType objectType)
    {
        Rectangle rectangle = GetCellRectangle(position, 11);
        DrawObjectIcon(graphics, objectType, rectangle);
    }

    private void DrawSnake(Graphics graphics)
    {
        for (int index = _game.Snake.Count - 1; index >= 0; index--)
        {
            Point position = _game.Snake[index];
            Rectangle rectangle = GetCellRectangle(position, 8);
            bool isHead = index == 0;
            Color segmentColor = isHead ? Color.FromArgb(39, 128, 73) : Color.FromArgb(70, 157, 91);
            using (SolidBrush snakeBrush = new SolidBrush(segmentColor))
            using (Pen snakePen = new Pen(Color.FromArgb(31, 101, 59), 3))
            {
                graphics.FillEllipse(snakeBrush, rectangle);
                graphics.DrawEllipse(snakePen, rectangle);
            }

            if (isHead)
            {
                DrawSnakeEyes(graphics, rectangle);
            }
        }
    }

    private void DrawSnakeEyes(Graphics graphics, Rectangle head)
    {
        Point firstEye = new Point(head.Left + head.Width / 3, head.Top + head.Height / 3);
        Point secondEye = new Point(head.Left + head.Width * 2 / 3, head.Top + head.Height / 3);
        using (SolidBrush eyeWhite = new SolidBrush(Color.FromArgb(246, 243, 217)))
        using (SolidBrush eyeDark = new SolidBrush(Color.FromArgb(32, 58, 48)))
        {
            graphics.FillEllipse(eyeWhite, firstEye.X - 5, firstEye.Y - 5, 10, 10);
            graphics.FillEllipse(eyeWhite, secondEye.X - 5, secondEye.Y - 5, 10, 10);
            graphics.FillEllipse(eyeDark, firstEye.X - 2, firstEye.Y - 2, 5, 5);
            graphics.FillEllipse(eyeDark, secondEye.X - 2, secondEye.Y - 2, 5, 5);
        }
    }

    private void DrawSidebar(Graphics graphics)
    {
        using (SolidBrush cardBrush = new SolidBrush(Color.FromArgb(255, 253, 246)))
        using (Pen cardPen = new Pen(Color.FromArgb(200, 215, 199), 3))
        {
            FillRoundedRectangle(graphics, cardBrush, SIDEBAR_X, SIDEBAR_Y, SIDEBAR_WIDTH, SIDEBAR_HEIGHT, 24);
            DrawRoundedRectangle(graphics, cardPen, SIDEBAR_X, SIDEBAR_Y, SIDEBAR_WIDTH, SIDEBAR_HEIGHT, 24);
        }

        if (_game.Phase == GamePhase.GameOver)
        {
            DrawGameOverPanel(graphics);
            return;
        }

        if (_game.Phase == GamePhase.Success)
        {
            DrawSuccessPanel(graphics);
            return;
        }

        bool showPreviousResult = _game.Phase == GamePhase.Ready && _game.HasPreviousLevelResult;
        if (showPreviousResult)
        {
            DrawPreviousResult(graphics);
        }

        float titleY = showPreviousResult ? 178 : 132;
        DrawText(graphics, $"第{_game.Level}關", SIDEBAR_X + 25, titleY, SIDEBAR_WIDTH - 50, 56, 42, Color.FromArgb(36, 61, 50), true, StringAlignment.Center);
        using (Pen separator = new Pen(Color.FromArgb(216, 225, 212), 2))
        {
            graphics.DrawLine(separator, SIDEBAR_X + 30, titleY + 62, SIDEBAR_X + SIDEBAR_WIDTH - 30, titleY + 62);
        }

        DrawLevelStatus(graphics, showPreviousResult ? 246 : 200);
        DrawRulesPanel(graphics);

        if (_game.Phase == GamePhase.Ready)
        {
            DrawActionButton(graphics, _startButton, "開始", Color.FromArgb(71, 123, 84));
        }
    }

    private void DrawPreviousResult(Graphics graphics)
    {
        Rectangle rectangle = new Rectangle(SIDEBAR_X + 30, SIDEBAR_Y + 12, SIDEBAR_WIDTH - 60, 52);
        using (SolidBrush resultBrush = new SolidBrush(Color.FromArgb(228, 240, 222)))
        using (Pen resultPen = new Pen(Color.FromArgb(164, 194, 157), 2))
        {
            FillRoundedRectangle(graphics, resultBrush, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, 12);
            DrawRoundedRectangle(graphics, resultPen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, 12);
        }

        DrawText(graphics, $"第{_game.LastCompletedLevel}關通關　通關蛇長：{_game.LastCompletedLength} 格", rectangle.X + 8, rectangle.Y + 4, rectangle.Width - 16, rectangle.Height - 8, 22, Color.FromArgb(46, 95, 57), true, StringAlignment.Center);
    }

    private void DrawLevelStatus(Graphics graphics, float startY)
    {
        DrawText(graphics, "蛇長", SIDEBAR_X + 44, startY, 160, 36, 26, Color.FromArgb(66, 88, 75), false, StringAlignment.Near);
        DrawText(graphics, $"{_game.SnakeLength} / {_game.TargetLength}", SIDEBAR_X + 44, startY + 29, 310, 69, 52, Color.FromArgb(36, 61, 50), true, StringAlignment.Near);
        DrawText(graphics, $"目標 {_game.TargetLength} 格", SIDEBAR_X + 330, startY + 49, 190, 36, 22, Color.FromArgb(66, 88, 75), false, StringAlignment.Far);

        int progress = _game.ProgressPercent;
        DrawText(graphics, $"過關進度　{progress}%", SIDEBAR_X + 44, startY + 94, SIDEBAR_WIDTH - 88, 34, 22, Color.FromArgb(66, 88, 75), false, StringAlignment.Near);
        Rectangle progressRectangle = new Rectangle(SIDEBAR_X + 44, (int)startY + 132, 478, 22);
        using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(225, 231, 223)))
        using (SolidBrush progressBrush = new SolidBrush(Color.FromArgb(91, 158, 104)))
        {
            FillRoundedRectangle(graphics, trackBrush, progressRectangle.X, progressRectangle.Y, progressRectangle.Width, progressRectangle.Height, 11);
            int fillWidth = progressRectangle.Width * progress / 100;
            if (fillWidth > 0)
            {
                FillRoundedRectangle(graphics, progressBrush, progressRectangle.X, progressRectangle.Y, fillWidth, progressRectangle.Height, 11);
            }
        }

        if (_game.Level >= 2)
        {
            DrawText(graphics, $"保命次數　{_game.ShieldCount}", SIDEBAR_X + 44, startY + 159, SIDEBAR_WIDTH - 88, 34, 22, Color.FromArgb(66, 88, 75), false, StringAlignment.Near);
        }
    }

    private void DrawRulesPanel(Graphics graphics)
    {
        Rectangle panel = new Rectangle(SIDEBAR_X + 30, 445, SIDEBAR_WIDTH - 60, 303);
        using (SolidBrush rulesBrush = new SolidBrush(Color.FromArgb(250, 251, 245)))
        using (Pen rulesPen = new Pen(Color.FromArgb(216, 225, 212), 2))
        {
            FillRoundedRectangle(graphics, rulesBrush, panel.X, panel.Y, panel.Width, panel.Height, 16);
            DrawRoundedRectangle(graphics, rulesPen, panel.X, panel.Y, panel.Width, panel.Height, 16);
        }

        DrawText(graphics, "規則提示", panel.X + 26, panel.Y + 12, panel.Width - 52, 34, 23, Color.FromArgb(66, 88, 75), true, StringAlignment.Near);
        List<(GameObjectType? Icon, string Text)> rules = GetRules();
        int rowY = panel.Y + 52;
        int rowHeight = rules.Count >= 6 ? 34 : 47;
        for (int index = 0; index < rules.Count; index++)
        {
            (GameObjectType? icon, string text) = rules[index];
            if (icon.HasValue)
            {
                DrawObjectIcon(graphics, icon.Value, new Rectangle(panel.X + 26, rowY + 2, 35, 35));
            }
            else
            {
                DrawRuleMarker(graphics, panel.X + 43, rowY + 19);
            }

            DrawText(graphics, text, panel.X + 75, rowY, panel.Width - 95, rowHeight, 21, Color.FromArgb(38, 59, 49), false, StringAlignment.Near);
            rowY += rowHeight;
            if (index < rules.Count - 1)
            {
                using Pen separator = new Pen(Color.FromArgb(228, 233, 223), 1);
                graphics.DrawLine(separator, panel.X + 24, rowY, panel.Right - 24, rowY);
            }
        }
    }

    private List<(GameObjectType? Icon, string Text)> GetRules()
    {
        List<(GameObjectType? Icon, string Text)> rules = new List<(GameObjectType? Icon, string Text)>();
        if (_game.Level == 1)
        {
            rules.Add((GameObjectType.Strawberry, "普通食物：草莓 +1 格"));
            rules.Add((null, "棋盤邊界：可以穿越"));
            rules.Add((null, "碰到自己的身體：死亡"));
        }
        else
        {
            rules.Add((GameObjectType.Strawberry, "普通食物：草莓 +1 格"));
            rules.Add((GameObjectType.Blueberry, "特殊食物：藍莓 +2 格"));
            rules.Add((GameObjectType.Shield, "護盾：+1 次保命"));
            rules.Add((GameObjectType.Fire, "火焰：蛇長 −2 格"));
            if (_game.Level == 3)
            {
                rules.Add((GameObjectType.Bomb, "炸彈：立即死亡"));
                rules.Add((null, "迷宮牆：不可穿越"));
                rules.Add((null, GetDoorStatusText()));
            }
        }

        return rules;
    }

    private string GetDoorStatusText()
    {
        string firstDoor = _game.IsDoorOpen(0) ? "開啟" : "關閉";
        string secondDoor = _game.IsDoorOpen(1) ? "開啟" : "關閉";
        return $"門狀態：門一{firstDoor}、門二{secondDoor}";
    }

    private void DrawRuleMarker(Graphics graphics, int centerX, int centerY)
    {
        using (SolidBrush markerBrush = new SolidBrush(Color.FromArgb(112, 139, 118)))
        {
            graphics.FillEllipse(markerBrush, centerX - 7, centerY - 7, 14, 14);
        }
    }

    private void DrawGameOverPanel(Graphics graphics)
    {
        DrawText(graphics, "Game Over", SIDEBAR_X + 30, SIDEBAR_Y + 74, SIDEBAR_WIDTH - 60, 66, 43, Color.FromArgb(140, 53, 47), true, StringAlignment.Center);
        DrawText(graphics, $"第 {_game.FailedLevel} 關失敗", SIDEBAR_X + 52, SIDEBAR_Y + 222, SIDEBAR_WIDTH - 104, 48, 27, Color.FromArgb(38, 59, 49), false, StringAlignment.Center);
        DrawText(graphics, $"最後蛇長：{_game.FinalLength} / {_game.TargetLength}", SIDEBAR_X + 52, SIDEBAR_Y + 284, SIDEBAR_WIDTH - 104, 58, 32, Color.FromArgb(36, 61, 50), true, StringAlignment.Center);
        DrawText(graphics, $"過關進度　{_game.ProgressPercent}%", SIDEBAR_X + 52, SIDEBAR_Y + 360, SIDEBAR_WIDTH - 104, 38, 22, Color.FromArgb(66, 88, 75), false, StringAlignment.Center);
        DrawProgressBar(graphics, SIDEBAR_X + 44, SIDEBAR_Y + 408, _game.ProgressPercent);
        if (_game.FailedByFire)
        {
            DrawText(graphics, "火焰致死進度為 0%", SIDEBAR_X + 52, SIDEBAR_Y + 450, SIDEBAR_WIDTH - 104, 36, 20, Color.FromArgb(102, 78, 62), false, StringAlignment.Center);
        }

        DrawActionButton(graphics, _replayButton, "重玩", Color.FromArgb(71, 123, 84));
    }

    private void DrawSuccessPanel(Graphics graphics)
    {
        DrawText(graphics, "闖關成功", SIDEBAR_X + 30, SIDEBAR_Y + 76, SIDEBAR_WIDTH - 60, 70, 45, Color.FromArgb(46, 111, 61), true, StringAlignment.Center);
        DrawText(graphics, $"蛇長：{_game.SnakeLength} / {_game.TargetLength}", SIDEBAR_X + 52, SIDEBAR_Y + 225, SIDEBAR_WIDTH - 104, 58, 32, Color.FromArgb(36, 61, 50), true, StringAlignment.Center);
        DrawText(graphics, "過關進度　100%", SIDEBAR_X + 52, SIDEBAR_Y + 306, SIDEBAR_WIDTH - 104, 38, 22, Color.FromArgb(66, 88, 75), false, StringAlignment.Center);
        DrawProgressBar(graphics, SIDEBAR_X + 44, SIDEBAR_Y + 354, 100);
        DrawActionButton(graphics, _replayButton, "重玩", Color.FromArgb(71, 123, 84));
    }

    private void DrawProgressBar(Graphics graphics, int x, int y, int percent)
    {
        using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(225, 231, 223)))
        using (SolidBrush progressBrush = new SolidBrush(Color.FromArgb(91, 158, 104)))
        {
            FillRoundedRectangle(graphics, trackBrush, x, y, 478, 22, 11);
            int fillWidth = 478 * Math.Clamp(percent, 0, 100) / 100;
            if (fillWidth > 0)
            {
                FillRoundedRectangle(graphics, progressBrush, x, y, fillWidth, 22, 11);
            }
        }
    }

    private void DrawActionButton(Graphics graphics, Rectangle rectangle, string label, Color fillColor)
    {
        using (SolidBrush buttonBrush = new SolidBrush(fillColor))
        using (Pen buttonPen = new Pen(Color.FromArgb(45, 96, 57), 2))
        {
            FillRoundedRectangle(graphics, buttonBrush, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, 14);
            DrawRoundedRectangle(graphics, buttonPen, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, 14);
        }

        DrawText(graphics, label, rectangle.X + 8, rectangle.Y + 5, rectangle.Width - 16, rectangle.Height - 10, 30, Color.White, true, StringAlignment.Center);
    }

    private void DrawObjectIcon(Graphics graphics, GameObjectType objectType, Rectangle rectangle)
    {
        if (objectType == GameObjectType.Strawberry)
        {
            DrawStrawberry(graphics, rectangle);
        }
        else if (objectType == GameObjectType.Blueberry)
        {
            DrawBlueberry(graphics, rectangle);
        }
        else if (objectType == GameObjectType.Shield)
        {
            DrawShield(graphics, rectangle);
        }
        else if (objectType == GameObjectType.Fire)
        {
            DrawFire(graphics, rectangle);
        }
        else if (objectType == GameObjectType.Bomb)
        {
            DrawBomb(graphics, rectangle);
        }
    }

    private void DrawStrawberry(Graphics graphics, Rectangle rectangle)
    {
        PointF[] berryPoints = new PointF[]
        {
            new PointF(rectangle.Left + rectangle.Width * 0.50f, rectangle.Top + rectangle.Height * 0.28f),
            new PointF(rectangle.Left + rectangle.Width * 0.88f, rectangle.Top + rectangle.Height * 0.34f),
            new PointF(rectangle.Left + rectangle.Width * 0.77f, rectangle.Top + rectangle.Height * 0.72f),
            new PointF(rectangle.Left + rectangle.Width * 0.50f, rectangle.Bottom - 1),
            new PointF(rectangle.Left + rectangle.Width * 0.23f, rectangle.Top + rectangle.Height * 0.72f),
            new PointF(rectangle.Left + rectangle.Width * 0.12f, rectangle.Top + rectangle.Height * 0.34f)
        };

        using (SolidBrush berryBrush = new SolidBrush(Color.FromArgb(227, 76, 85)))
        using (Pen berryPen = new Pen(Color.FromArgb(169, 45, 58), 2))
        using (SolidBrush leafBrush = new SolidBrush(Color.FromArgb(79, 157, 84)))
        using (SolidBrush seedBrush = new SolidBrush(Color.FromArgb(255, 241, 199)))
        {
            graphics.FillPolygon(berryBrush, berryPoints);
            graphics.DrawPolygon(berryPen, berryPoints);
            graphics.FillEllipse(leafBrush, rectangle.Left + rectangle.Width * 0.25f, rectangle.Top, rectangle.Width * 0.24f, rectangle.Height * 0.26f);
            graphics.FillEllipse(leafBrush, rectangle.Left + rectangle.Width * 0.52f, rectangle.Top, rectangle.Width * 0.24f, rectangle.Height * 0.26f);
            graphics.FillEllipse(seedBrush, rectangle.Left + rectangle.Width * 0.34f, rectangle.Top + rectangle.Height * 0.42f, 3, 5);
            graphics.FillEllipse(seedBrush, rectangle.Left + rectangle.Width * 0.61f, rectangle.Top + rectangle.Height * 0.44f, 3, 5);
            graphics.FillEllipse(seedBrush, rectangle.Left + rectangle.Width * 0.47f, rectangle.Top + rectangle.Height * 0.66f, 3, 5);
        }
    }

    private void DrawBlueberry(Graphics graphics, Rectangle rectangle)
    {
        float diameter = rectangle.Width * 0.54f;
        using (SolidBrush berryBrush = new SolidBrush(Color.FromArgb(63, 91, 168)))
        using (Pen berryPen = new Pen(Color.FromArgb(45, 64, 126), 2))
        using (SolidBrush leafBrush = new SolidBrush(Color.FromArgb(79, 157, 84)))
        {
            graphics.FillEllipse(berryBrush, rectangle.Left + 1, rectangle.Top + rectangle.Height * 0.30f, diameter, diameter);
            graphics.DrawEllipse(berryPen, rectangle.Left + 1, rectangle.Top + rectangle.Height * 0.30f, diameter, diameter);
            graphics.FillEllipse(berryBrush, rectangle.Left + rectangle.Width * 0.38f, rectangle.Top + rectangle.Height * 0.27f, diameter, diameter);
            graphics.DrawEllipse(berryPen, rectangle.Left + rectangle.Width * 0.38f, rectangle.Top + rectangle.Height * 0.27f, diameter, diameter);
            graphics.FillEllipse(leafBrush, rectangle.Left + rectangle.Width * 0.35f, rectangle.Top + 1, rectangle.Width * 0.30f, rectangle.Height * 0.22f);
        }
    }

    private void DrawShield(Graphics graphics, Rectangle rectangle)
    {
        PointF[] shieldPoints = new PointF[]
        {
            new PointF(rectangle.Left + rectangle.Width * 0.50f, rectangle.Top + 1),
            new PointF(rectangle.Right - 2, rectangle.Top + rectangle.Height * 0.20f),
            new PointF(rectangle.Right - 5, rectangle.Top + rectangle.Height * 0.65f),
            new PointF(rectangle.Left + rectangle.Width * 0.50f, rectangle.Bottom - 1),
            new PointF(rectangle.Left + 5, rectangle.Top + rectangle.Height * 0.65f),
            new PointF(rectangle.Left + 2, rectangle.Top + rectangle.Height * 0.20f)
        };

        using (SolidBrush shieldBrush = new SolidBrush(Color.FromArgb(76, 137, 188)))
        using (Pen shieldPen = new Pen(Color.FromArgb(42, 87, 130), 3))
        using (Pen emblemPen = new Pen(Color.FromArgb(239, 247, 250), 3))
        {
            graphics.FillPolygon(shieldBrush, shieldPoints);
            graphics.DrawPolygon(shieldPen, shieldPoints);
            graphics.DrawLine(emblemPen, rectangle.Left + rectangle.Width / 2, rectangle.Top + rectangle.Height * 0.23f, rectangle.Left + rectangle.Width / 2, rectangle.Top + rectangle.Height * 0.72f);
        }
    }

    private void DrawFire(Graphics graphics, Rectangle rectangle)
    {
        PointF[] firePoints = new PointF[]
        {
            new PointF(rectangle.Left + rectangle.Width * 0.50f, rectangle.Top),
            new PointF(rectangle.Left + rectangle.Width * 0.70f, rectangle.Top + rectangle.Height * 0.35f),
            new PointF(rectangle.Right - 1, rectangle.Top + rectangle.Height * 0.42f),
            new PointF(rectangle.Left + rectangle.Width * 0.80f, rectangle.Bottom - 1),
            new PointF(rectangle.Left + rectangle.Width * 0.28f, rectangle.Bottom - 1),
            new PointF(rectangle.Left + 1, rectangle.Top + rectangle.Height * 0.48f),
            new PointF(rectangle.Left + rectangle.Width * 0.37f, rectangle.Top + rectangle.Height * 0.41f)
        };

        using (SolidBrush fireBrush = new SolidBrush(Color.FromArgb(238, 115, 46)))
        using (SolidBrush coreBrush = new SolidBrush(Color.FromArgb(255, 204, 83)))
        {
            graphics.FillPolygon(fireBrush, firePoints);
            graphics.FillEllipse(coreBrush, rectangle.Left + rectangle.Width * 0.35f, rectangle.Top + rectangle.Height * 0.52f, rectangle.Width * 0.30f, rectangle.Height * 0.36f);
        }
    }

    private void DrawBomb(Graphics graphics, Rectangle rectangle)
    {
        Rectangle body = new Rectangle(rectangle.X + rectangle.Width / 8, rectangle.Y + rectangle.Height / 5, rectangle.Width * 3 / 4, rectangle.Height * 3 / 4);
        using (SolidBrush bombBrush = new SolidBrush(Color.FromArgb(43, 47, 48)))
        using (Pen bombPen = new Pen(Color.FromArgb(24, 27, 28), 2))
        using (Pen fusePen = new Pen(Color.FromArgb(133, 91, 53), 3))
        using (SolidBrush sparkBrush = new SolidBrush(Color.FromArgb(240, 170, 53)))
        {
            graphics.FillEllipse(bombBrush, body);
            graphics.DrawEllipse(bombPen, body);
            graphics.DrawLine(fusePen, body.Left + body.Width * 0.62f, body.Top + 2, body.Right - 2, rectangle.Top + 1);
            graphics.FillEllipse(sparkBrush, rectangle.Right - 8, rectangle.Top + 1, 6, 6);
        }
    }

    private Rectangle GetCellRectangle(Point position, int inset)
    {
        return new Rectangle(BOARD_X + position.X * CELL_SIZE + inset, BOARD_Y + position.Y * CELL_SIZE + inset, CELL_SIZE - inset * 2, CELL_SIZE - inset * 2);
    }

    private void DrawText(Graphics graphics, string text, float x, float y, float width, float height, float fontSize, Color color, bool bold, StringAlignment alignment)
    {
        _renderedText.Add(text);
        FontStyle style = bold ? FontStyle.Bold : FontStyle.Regular;
        using (Font font = new Font("Microsoft JhengHei", fontSize, style, GraphicsUnit.Pixel))
        using (SolidBrush brush = new SolidBrush(color))
        using (StringFormat format = new StringFormat())
        {
            format.Alignment = alignment;
            format.LineAlignment = StringAlignment.Center;
            format.Trimming = StringTrimming.EllipsisCharacter;
            graphics.DrawString(text, font, brush, new RectangleF(x, y, width, height), format);
        }
    }

    private void FillRoundedRectangle(Graphics graphics, Brush brush, int x, int y, int width, int height, int radius)
    {
        using (GraphicsPath path = CreateRoundedRectanglePath(x, y, width, height, radius))
        {
            graphics.FillPath(brush, path);
        }
    }

    private void DrawRoundedRectangle(Graphics graphics, Pen pen, int x, int y, int width, int height, int radius)
    {
        using (GraphicsPath path = CreateRoundedRectanglePath(x, y, width, height, radius))
        {
            graphics.DrawPath(pen, path);
        }
    }

    private GraphicsPath CreateRoundedRectanglePath(int x, int y, int width, int height, int radius)
    {
        GraphicsPath path = new GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void OnFrameTimerTick(object? sender, EventArgs eventArgs)
    {
        if (_faulted)
        {
            return;
        }

        int priorLevel = _game.Level;
        string contractMethod = priorLevel == 1 ? nameof(SnakeGame.AdvanceFirstLevelStep) : priorLevel == 2 ? nameof(SnakeGame.AdvanceSecondLevelStep) : nameof(SnakeGame.AdvanceThirdLevelStep);
        ExecuteUiOperation(contractMethod, () =>
        {
            _game.UpdateGameTime();
            if (_game.Phase == GamePhase.DeathPending)
            {
                _game.ShowGameOverScreen();
            }
            else if (_game.Phase == GamePhase.SuccessPending)
            {
                _game.ShowThirdLevelSuccessScreen();
            }
            else if (_game.Level == 2 && priorLevel == 1)
            {
                _game.ShowSecondLevelReadyScreen();
            }
            else if (_game.Level == 3 && priorLevel == 2)
            {
                _game.ShowThirdLevelReadyScreen();
            }

            _game.UpdateLevelStatus(_game.Level);
        });
    }

    private void OnGameKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        Direction? direction = eventArgs.KeyCode switch
        {
            Keys.Up => Direction.Up,
            Keys.Down => Direction.Down,
            Keys.Left => Direction.Left,
            Keys.Right => Direction.Right,
            _ => null
        };

        if (!direction.HasValue)
        {
            return;
        }

        eventArgs.Handled = true;
        eventArgs.SuppressKeyPress = true;
        if (_faulted)
        {
            return;
        }

        ExecuteUiOperation(nameof(SnakeGame.SetDirection), () => _game.SetDirection(direction.Value));
    }

    private void OnGameMouseDown(object? sender, MouseEventArgs eventArgs)
    {
        if (_faulted || _renderScale <= 0)
        {
            return;
        }

        Point logicalPosition = new Point((int)((eventArgs.X - _renderOffsetX) / _renderScale), (int)((eventArgs.Y - _renderOffsetY) / _renderScale));
        if (_game.Phase == GamePhase.Ready && _startButton.Contains(logicalPosition))
        {
            StartCurrentLevel();
        }
        else if ((_game.Phase == GamePhase.GameOver || _game.Phase == GamePhase.Success) && _replayButton.Contains(logicalPosition))
        {
            ExecuteUiOperation(nameof(SnakeGame.RestartGame), () =>
            {
                _game.RestartGame();
                _game.UpdateLevelStatus(1);
            });
        }
    }

    private void StartCurrentLevel()
    {
        int level = _game.Level;
        string contractMethod = level == 1 ? nameof(SnakeGame.StartFirstLevel) : level == 2 ? nameof(SnakeGame.StartSecondLevel) : nameof(SnakeGame.StartThirdLevel);
        ExecuteUiOperation(contractMethod, () =>
        {
            if (level == 1)
            {
                _game.StartFirstLevel();
            }
            else if (level == 2)
            {
                _game.StartSecondLevel();
            }
            else if (level == 3)
            {
                _game.StartThirdLevel();
            }

            _game.UpdateLevelStatus(level);
        });
    }

    private void ExecuteUiOperation(string contractMethod, Action operation)
    {
        try
        {
            operation();
            VerifyVisibleContract();
            Invalidate();
        }
        catch (ArgumentOutOfRangeException exception) when (IsContractViolation(exception))
        {
            HandleContractViolation(contractMethod, exception);
        }
        catch (InvalidOperationException exception) when (IsContractViolation(exception))
        {
            HandleContractViolation(contractMethod, exception);
        }
        catch (OutputContractViolationException exception)
        {
            HandleStoppedGameFailure(contractMethod, exception);
        }
        catch (Exception exception)
        {
            HandleStoppedGameFailure(contractMethod, exception);
        }
    }

    private bool IsContractViolation(Exception exception)
    {
        return exception.Data.Contains("Snake.ContractViolation") && exception.Data["Snake.ContractViolation"] is bool isViolation && isViolation;
    }

    private void VerifyVisibleContract()
    {
        string contractMethod = _game.Phase switch
        {
            GamePhase.Ready when _game.Level == 1 => nameof(SnakeGame.ShowFirstLevelReadyScreen),
            GamePhase.Ready when _game.Level == 2 => nameof(SnakeGame.ShowSecondLevelReadyScreen),
            GamePhase.Ready when _game.Level == 3 => nameof(SnakeGame.ShowThirdLevelReadyScreen),
            GamePhase.Playing when _game.Level == 1 => nameof(SnakeGame.StartFirstLevel),
            GamePhase.Playing when _game.Level == 2 => nameof(SnakeGame.StartSecondLevel),
            GamePhase.Playing when _game.Level == 3 => nameof(SnakeGame.StartThirdLevel),
            GamePhase.GameOver => nameof(SnakeGame.ShowGameOverScreen),
            GamePhase.Success => nameof(SnakeGame.ShowThirdLevelSuccessScreen),
            _ => "GameScreen"
        };

        bool isValid = _game.StatusLevel == _game.Level && _game.ProgressPercent >= 0 && _game.ProgressPercent <= 100;
        if (_game.Phase == GamePhase.Ready)
        {
            isValid = isValid && _game.SnakeLength == 3 && _game.Snake.Count == 3 && _game.Objects.Count == 0;
            if (_game.Level >= 2)
            {
                isValid = isValid && _game.ShieldCount == 0 && _game.HasPreviousLevelResult;
            }
        }
        else if (_game.Phase == GamePhase.Playing)
        {
            isValid = isValid && _game.Objects.Count > 0;
        }
        else if (_game.Phase == GamePhase.GameOver)
        {
            isValid = isValid && _game.FailedLevel >= 1 && _game.FailedLevel <= 3;
        }
        else if (_game.Phase == GamePhase.Success)
        {
            isValid = isValid && _game.Level == 3 && _game.SnakeLength >= 50 && _game.ProgressPercent == 100;
        }

        if (!isValid)
        {
            throw new OutputContractViolationException(contractMethod, "實際畫面狀態符合關卡、蛇長、進度、物件與按鈕的可見保證", $"level={_game.Level}, statusLevel={_game.StatusLevel}, phase={_game.Phase}, length={_game.SnakeLength}, progress={_game.ProgressPercent}, objects={_game.Objects.Count}");
        }
    }

    private void HandleContractViolation(string contractMethod, Exception exception)
    {
        RecordDiagnostic(contractMethod, exception);
        MessageBox.Show(this, "目前無法執行此操作", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        Invalidate();
    }

    private void HandleStoppedGameFailure(string contractMethod, Exception exception)
    {
        _faulted = true;
        _frameTimer.Stop();
        RecordDiagnostic(contractMethod, exception);
        MessageBox.Show(this, "遊戲發生錯誤，本輪已停止", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void HandleUnrecognizedFailure(string contractMethod, Exception exception)
    {
        if (_faulted)
        {
            return;
        }

        HandleStoppedGameFailure(contractMethod, exception);
    }

    private void RecordDiagnostic(string contractMethod, Exception exception)
    {
        Trace.WriteLine($"[{DateTime.Now:O}] 合約方法：{contractMethod}；例外：{exception.GetType().FullName}；診斷：{exception}");
    }

    private void OnGameFormClosed(object? sender, FormClosedEventArgs eventArgs)
    {
        _frameTimer.Stop();
        _frameTimer.Dispose();
    }
}
