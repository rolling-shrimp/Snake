using System.Drawing.Drawing2D;
using Snake.Application._GetGameScreenUseCase.Enums;
using Snake.Application._GetGameScreenUseCase;
using Snake.Application._GetGameScreenUseCase.Dto;

namespace Snake.WinForms
{
    public partial class GameForm
    {
        private const int BOARD_SIZE = 10;
        private const int CELL_BITMAP_SIZE = 128;
        private TableLayoutPanel _mainLayout = null!;
        private Panel _boardSurface = null!;
        private TableLayoutPanel _boardGrid = null!;
        private PictureBox[,] _boardCells = null!;
        private Bitmap[,] _boardCellImages = null!;
        private FlowLayoutPanel _sidebar = null!;
        private Label _levelLabel = null!;
        private Label _lengthLabel = null!;
        private Panel _shieldPanel = null!;
        private Label _shieldLabel = null!;
        private Panel _progressSurface = null!;
        private Label _progressLabel = null!;
        private Panel _rulesPanel = null!;
        private List<Label> _ruleLabels = null!;
        private Label _doorLabel = null!;
        private Panel _resultPanel = null!;
        private Label _resultTitleLabel = null!;
        private Label _resultMessageLabel = null!;
        private Button _startButton = null!;
        private Button _replayButton = null!;
        private Label _errorLabel = null!;
        private GameScreenDTO? _screen;
        private int _progressPercent;

        private void InitializeRendering()
        {
            _mainLayout = new TableLayoutPanel();
            _boardSurface = new Panel();
            _boardGrid = new TableLayoutPanel();
            _boardCells = new PictureBox[BOARD_SIZE, BOARD_SIZE];
            _boardCellImages = new Bitmap[BOARD_SIZE, BOARD_SIZE];
            _sidebar = new FlowLayoutPanel();
            _levelLabel = CreateLabel(34, FontStyle.Bold, 68, ContentAlignment.MiddleCenter);
            _lengthLabel = CreateLabel(34, FontStyle.Bold, 52, ContentAlignment.MiddleLeft);
            _shieldPanel = CreateCardPanel();
            _shieldLabel = CreateLabel(21, FontStyle.Bold, 48, ContentAlignment.MiddleCenter);
            _progressSurface = new Panel();
            _progressLabel = CreateLabel(12, FontStyle.Bold, 32, ContentAlignment.MiddleLeft);
            _rulesPanel = CreateCardPanel();
            _ruleLabels = new List<Label>();
            _doorLabel = CreateLabel(11, FontStyle.Bold, 32, ContentAlignment.MiddleLeft);
            _resultPanel = CreateCardPanel();
            _resultTitleLabel = CreateLabel(22, FontStyle.Bold, 42, ContentAlignment.MiddleCenter);
            _resultMessageLabel = CreateLabel(12, FontStyle.Bold, 68, ContentAlignment.TopCenter);
            _startButton = CreateActionButton("開始");
            _replayButton = CreateActionButton("重玩");
            _errorLabel = CreateLabel(11, FontStyle.Bold, 52, ContentAlignment.MiddleCenter);
            BuildLayout();
        }

        private void ApplyScreen(GameScreenDTO screen)
        {
            ArgumentNullException.ThrowIfNull(screen);
            GameScreenDTO? previousScreen = _screen;
            _screen = screen;
            if (_errorLabel.Visible)
            {
                _errorLabel.Visible = false;
            }

            bool boardVisible = screen.ScreenKind != GameScreenKind.GameOver;
            if (_boardSurface.Visible != boardVisible || previousScreen is null)
            {
                _boardSurface.Visible = boardVisible;
                _mainLayout.ColumnStyles[0].Width = boardVisible ? 67 : 0;
                _mainLayout.ColumnStyles[1].Width = boardVisible ? 33 : 100;
            }

            if (previousScreen is null || HasStatusChanged(previousScreen.LevelStatus, screen.LevelStatus))
            {
                RenderStatus(screen.LevelStatus);
            }

            if (previousScreen is null || HaveRulesChanged(previousScreen, screen))
            {
                RenderRules(screen);
            }

            if (previousScreen is null || HasResultChanged(previousScreen.Result, screen.Result))
            {
                RenderResult(screen.Result);
            }

            bool canStart = screen.AvailableActions.Contains(GameAction.Start);
            if (_startButton.Visible != canStart || _startButton.Enabled != canStart)
            {
                _startButton.Visible = canStart;
                _startButton.Enabled = canStart;
            }

            bool canRestart = screen.AvailableActions.Contains(GameAction.Restart);
            if (_replayButton.Visible != canRestart || _replayButton.Enabled != canRestart)
            {
                _replayButton.Visible = canRestart;
                _replayButton.Enabled = canRestart;
            }

            for (int y = 0; y < BOARD_SIZE; y++)
            {
                for (int x = 0; x < BOARD_SIZE; x++)
                {
                    if (previousScreen is null || HasCellChanged(previousScreen, screen, x, y))
                    {
                        DrawBoardCell(screen, x, y);
                    }
                }
            }
        }

        private void ShowFailure(string message)
        {
            _errorLabel.Text = message;
            _errorLabel.Visible = true;
            _startButton.Enabled = false;
            _replayButton.Enabled = false;
        }

        private void BuildLayout()
        {
            _mainLayout.Dock = DockStyle.Fill;
            _mainLayout.ColumnCount = 2;
            _mainLayout.RowCount = 1;
            _mainLayout.Padding = new Padding(28, 24, 28, 28);
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67));
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _boardSurface.Dock = DockStyle.Fill;
            _boardSurface.Margin = new Padding(0, 0, 20, 0);
            _boardSurface.BackColor = Color.FromArgb(63, 126, 104);
            _boardSurface.AccessibleName = "遊戲棋盤";
            _boardSurface.Resize += OnBoardSurfaceResize;
            BuildBoardGrid();

            _sidebar.Dock = DockStyle.Fill;
            _sidebar.FlowDirection = FlowDirection.TopDown;
            _sidebar.WrapContents = false;
            _sidebar.AutoScroll = true;
            _sidebar.Padding = new Padding(12, 4, 12, 12);
            _sidebar.BackColor = Color.FromArgb(255, 253, 242);
            _sidebar.AccessibleName = "關卡資訊";

            _levelLabel.BackColor = Color.FromArgb(211, 247, 190);
            _levelLabel.ForeColor = Color.FromArgb(13, 58, 107);
            _levelLabel.Margin = new Padding(0, 0, 0, 14);
            _levelLabel.AccessibleName = "目前關卡";
            Panel statusPanel = BuildStatusPanel();
            BuildRulesPanel();
            BuildResultPanel();

            _startButton.AccessibleName = "開始目前關卡";
            _replayButton.AccessibleName = "重新開始遊戲";
            _errorLabel.ForeColor = Color.FromArgb(170, 48, 48);
            _errorLabel.BackColor = Color.FromArgb(255, 235, 230);
            _errorLabel.Visible = false;

            _sidebar.Controls.Add(_resultPanel);
            _sidebar.Controls.Add(_levelLabel);
            _sidebar.Controls.Add(statusPanel);
            _sidebar.Controls.Add(_rulesPanel);
            _sidebar.Controls.Add(_startButton);
            _sidebar.Controls.Add(_replayButton);
            _sidebar.Controls.Add(_errorLabel);
            _mainLayout.Controls.Add(_boardSurface, 0, 0);
            _mainLayout.Controls.Add(_sidebar, 1, 0);
            Controls.Add(_mainLayout);
        }

        private Panel BuildStatusPanel()
        {
            Panel statusPanel = CreateCardPanel();
            statusPanel.Height = 174;
            statusPanel.Padding = new Padding(18, 12, 18, 12);
            statusPanel.Margin = new Padding(0, 0, 0, 14);
            statusPanel.AccessibleName = "關卡狀態";
            TableLayoutPanel statusLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

            Panel lengthPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            Label lengthCaption = CreateLabel(11, FontStyle.Bold, 28, ContentAlignment.BottomLeft);
            lengthCaption.Text = "目前蛇長";
            lengthCaption.ForeColor = Color.FromArgb(49, 83, 117);
            _lengthLabel.Dock = DockStyle.Bottom;
            _lengthLabel.ForeColor = Color.FromArgb(15, 55, 101);
            lengthPanel.Controls.Add(_lengthLabel);
            lengthPanel.Controls.Add(lengthCaption);

            _shieldPanel.Dock = DockStyle.Fill;
            _shieldPanel.Margin = new Padding(10, 0, 0, 0);
            Label shieldCaption = CreateLabel(10, FontStyle.Bold, 28, ContentAlignment.BottomCenter);
            shieldCaption.Text = "護盾保命";
            shieldCaption.ForeColor = Color.FromArgb(49, 83, 117);
            _shieldLabel.Dock = DockStyle.Bottom;
            _shieldLabel.ForeColor = Color.FromArgb(15, 55, 101);
            _shieldPanel.Controls.Add(_shieldLabel);
            _shieldPanel.Controls.Add(shieldCaption);

            _progressSurface.Dock = DockStyle.Fill;
            _progressSurface.Margin = new Padding(0, 3, 0, 3);
            _progressSurface.AccessibleName = "過關進度";
            _progressSurface.Paint += OnProgressPaint;
            _progressLabel.Dock = DockStyle.Fill;
            _progressLabel.ForeColor = Color.FromArgb(38, 76, 115);
            statusLayout.Controls.Add(lengthPanel, 0, 0);
            statusLayout.Controls.Add(_shieldPanel, 1, 0);
            statusLayout.Controls.Add(_progressSurface, 0, 1);
            statusLayout.SetColumnSpan(_progressSurface, 2);
            statusLayout.Controls.Add(_progressLabel, 0, 2);
            statusLayout.SetColumnSpan(_progressLabel, 2);
            statusPanel.Controls.Add(statusLayout);
            return statusPanel;
        }

        private void BuildRulesPanel()
        {
            _rulesPanel.Height = 286;
            _rulesPanel.Padding = new Padding(16, 12, 16, 12);
            _rulesPanel.Margin = new Padding(0, 0, 0, 14);
            _rulesPanel.AccessibleName = "規則提示";
            Label rulesTitle = CreateLabel(16, FontStyle.Bold, 36, ContentAlignment.MiddleLeft);
            rulesTitle.Text = "規則提示";
            rulesTitle.Dock = DockStyle.Top;
            rulesTitle.ForeColor = Color.FromArgb(33, 71, 110);
            FlowLayoutPanel rulesList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                AutoScroll = true
            };
            for (int index = 0; index < 8; index++)
            {
                Label ruleLabel = CreateLabel(11, FontStyle.Bold, 30, ContentAlignment.MiddleLeft);
                ruleLabel.ForeColor = Color.FromArgb(45, 78, 110);
                ruleLabel.Visible = false;
                _ruleLabels.Add(ruleLabel);
                rulesList.Controls.Add(ruleLabel);
            }

            _doorLabel.ForeColor = Color.FromArgb(176, 112, 0);
            _doorLabel.Visible = false;
            rulesList.Controls.Add(_doorLabel);
            _rulesPanel.Controls.Add(rulesList);
            _rulesPanel.Controls.Add(rulesTitle);
        }

        private void BuildResultPanel()
        {
            _resultPanel.Height = 126;
            _resultPanel.Padding = new Padding(16, 10, 16, 8);
            _resultPanel.Margin = new Padding(0, 0, 0, 14);
            _resultPanel.BackColor = Color.FromArgb(231, 250, 211);
            _resultPanel.Visible = false;
            _resultPanel.AccessibleName = "遊戲結果";
            _resultTitleLabel.Dock = DockStyle.Top;
            _resultTitleLabel.ForeColor = Color.FromArgb(15, 55, 101);
            _resultMessageLabel.Dock = DockStyle.Fill;
            _resultMessageLabel.ForeColor = Color.FromArgb(38, 76, 115);
            _resultPanel.Controls.Add(_resultMessageLabel);
            _resultPanel.Controls.Add(_resultTitleLabel);
        }

        private void RenderStatus(LevelStatusDTO status)
        {
            bool progressChanged = _progressPercent != status.ProgressPercentage;
            _levelLabel.Text = status.LevelNumber switch
            {
                1 => "第一關",
                2 => "第二關",
                3 => "第三關",
                _ => $"第 {status.LevelNumber} 關"
            };
            _lengthLabel.Text = $"{status.CurrentLength} / {status.TargetLength}";
            _shieldPanel.Visible = status.LevelNumber != 1;
            _shieldLabel.Text = $"{status.RemainingShieldCharges} 次";
            _progressPercent = status.ProgressPercentage;
            _progressLabel.Text = $"過關進度 {status.ProgressPercentage}%     目標 {status.TargetLength} 格";
            if (progressChanged)
            {
                _progressSurface.Invalidate();
            }
        }

        private void RenderRules(GameScreenDTO screen)
        {
            for (int index = 0; index < _ruleLabels.Count; index++)
            {
                bool visible = index < screen.RuleHints.Count;
                _ruleLabels[index].Visible = visible;
                _ruleLabels[index].Text = visible ? $"{GetRuleGlyph(screen.RuleHints[index])}  {screen.RuleHints[index]}" : string.Empty;
            }

            DoorDTO? door = screen.Doors.FirstOrDefault();
            _doorLabel.Visible = door is not null;
            _doorLabel.Text = door is null ? string.Empty : $"門目前：{GetDoorStateText(door.State)}";
        }

        private void RenderResult(ResultCardDTO? result)
        {
            _resultPanel.Visible = result is not null;
            if (result is null)
            {
                return;
            }

            _resultTitleLabel.Text = result.Outcome switch
            {
                "Failed" => "Game Over",
                "Passed" when result.LevelNumber == 3 => "闖關成功",
                "Passed" => $"第 {result.LevelNumber} 關過關",
                _ => result.Outcome
            };
            string detail = string.IsNullOrWhiteSpace(result.DeathCause)
                ? $"最後蛇長：{result.FinalLength} 格"
                : GetDeathCauseText(result.DeathCause);
            _resultMessageLabel.Text = detail;
        }

        private void BuildBoardGrid()
        {
            _boardGrid.ColumnCount = BOARD_SIZE;
            _boardGrid.RowCount = BOARD_SIZE;
            _boardGrid.Margin = new Padding(0);
            _boardGrid.Padding = new Padding(0);
            _boardGrid.BackColor = Color.FromArgb(63, 126, 104);
            for (int index = 0; index < BOARD_SIZE; index++)
            {
                _boardGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
                _boardGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            }

            _boardGrid.SuspendLayout();
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                for (int x = 0; x < BOARD_SIZE; x++)
                {
                    Bitmap cellImage = new Bitmap(CELL_BITMAP_SIZE, CELL_BITMAP_SIZE);
                    PictureBox cell = new PictureBox
                    {
                        Dock = DockStyle.Fill,
                        Margin = new Padding(0),
                        Padding = new Padding(0),
                        Image = cellImage,
                        SizeMode = PictureBoxSizeMode.StretchImage,
                        TabStop = false,
                        AccessibleName = $"棋盤格 {x},{y}",
                        AccessibleDescription = $"第 {y + 1} 列，第 {x + 1} 欄"
                    };
                    _boardCells[x, y] = cell;
                    _boardCellImages[x, y] = cellImage;
                    DrawCellBackground(cellImage, x, y);
                    _boardGrid.Controls.Add(cell, x, y);
                }
            }

            _boardGrid.ResumeLayout(false);
            _boardSurface.Controls.Add(_boardGrid);
        }

        private void OnBoardSurfaceResize(object? sender, EventArgs eventArgs)
        {
            int availableSize = Math.Min(_boardSurface.ClientSize.Width, _boardSurface.ClientSize.Height) - 24;
            int boardLength = Math.Max(1, availableSize);
            _boardGrid.SetBounds(
                (_boardSurface.ClientSize.Width - boardLength) / 2,
                (_boardSurface.ClientSize.Height - boardLength) / 2,
                boardLength,
                boardLength);
        }

        private bool HasStatusChanged(LevelStatusDTO previous, LevelStatusDTO current)
        {
            return previous.LevelNumber != current.LevelNumber
                || previous.CurrentLength != current.CurrentLength
                || previous.TargetLength != current.TargetLength
                || previous.ProgressPercentage != current.ProgressPercentage
                || previous.RemainingShieldCharges != current.RemainingShieldCharges;
        }

        private bool HaveRulesChanged(GameScreenDTO previous, GameScreenDTO current)
        {
            if (!previous.RuleHints.SequenceEqual(current.RuleHints))
            {
                return true;
            }

            if (previous.Doors.Count != current.Doors.Count)
            {
                return true;
            }

            for (int index = 0; index < previous.Doors.Count; index++)
            {
                DoorDTO previousDoor = previous.Doors[index];
                DoorDTO currentDoor = current.Doors[index];
                if (previousDoor.Position.X != currentDoor.Position.X
                    || previousDoor.Position.Y != currentDoor.Position.Y
                    || previousDoor.State != currentDoor.State)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasResultChanged(ResultCardDTO? previous, ResultCardDTO? current)
        {
            if (previous is null || current is null)
            {
                return previous is not null || current is not null;
            }

            return previous.Outcome != current.Outcome
                || previous.LevelNumber != current.LevelNumber
                || previous.FinalLength != current.FinalLength
                || previous.DeathCause != current.DeathCause;
        }

        private bool HasCellChanged(GameScreenDTO previous, GameScreenDTO current, int x, int y)
        {
            BoardCellKind previousKind = GetBoardCellKind(previous, x, y);
            BoardCellKind currentKind = GetBoardCellKind(current, x, y);
            if (previousKind != currentKind)
            {
                return true;
            }

            return GetDoorState(previous, x, y) != GetDoorState(current, x, y);
        }

        private BoardCellKind GetBoardCellKind(GameScreenDTO screen, int x, int y)
        {
            BoardCellDTO? cell = screen.BoardCells.FirstOrDefault(boardCell =>
                boardCell.Position.X == x && boardCell.Position.Y == y);
            return cell?.Kind ?? BoardCellKind.Empty;
        }

        private DoorVisualState? GetDoorState(GameScreenDTO screen, int x, int y)
        {
            DoorDTO? door = screen.Doors.FirstOrDefault(item =>
                item.Position.X == x && item.Position.Y == y);
            return door?.State;
        }

        private void DrawBoardCell(GameScreenDTO screen, int x, int y)
        {
            Bitmap cellImage = _boardCellImages[x, y];
            DrawCellBackground(cellImage, x, y);
            using Graphics graphics = Graphics.FromImage(cellImage);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (screen.WallCells.Any(position => position.X == x && position.Y == y))
            {
                DrawWall(graphics);
            }

            DoorDTO? door = screen.Doors.FirstOrDefault(item =>
                item.Position.X == x && item.Position.Y == y);
            if (door is not null)
            {
                DrawDoor(graphics, door.State);
            }

            BoardObjectDTO? gameObject = screen.Objects.FirstOrDefault(item =>
                item.Position.X == x && item.Position.Y == y);
            if (gameObject is not null)
            {
                DrawObject(graphics, gameObject.Kind);
            }

            int snakeIndex = screen.SnakeCells.FindIndex(position => position.X == x && position.Y == y);
            if (snakeIndex >= 0)
            {
                DrawSnakeSegment(graphics, snakeIndex == 0);
            }

            _boardCells[x, y].Invalidate();
        }

        private void DrawCellBackground(Bitmap cellImage, int x, int y)
        {
            using Graphics graphics = Graphics.FromImage(cellImage);
            graphics.Clear((x + y) % 2 == 0
                ? Color.FromArgb(255, 249, 223)
                : Color.FromArgb(223, 245, 216));
        }

        private void DrawWall(Graphics graphics)
        {
            RectangleF cell = GetCellRectangle(0.08f);
            using SolidBrush wallBrush = new SolidBrush(Color.FromArgb(113, 139, 158));
            using Pen highlightPen = new Pen(Color.FromArgb(150, 225, 235, 240), CELL_BITMAP_SIZE * 0.045f);
            graphics.FillRectangle(wallBrush, cell);
            graphics.DrawLine(
                highlightPen,
                cell.Left + cell.Width * 0.12f,
                cell.Top + cell.Height * 0.24f,
                cell.Right - cell.Width * 0.12f,
                cell.Top + cell.Height * 0.24f);
        }

        private void DrawDoor(Graphics graphics, DoorVisualState state)
        {
            RectangleF cell = GetCellRectangle(0.1f);
            if (state is DoorVisualState.Open or DoorVisualState.CloseDeferred)
            {
                using Pen openPen = new Pen(Color.FromArgb(200, 137, 5), CELL_BITMAP_SIZE * 0.07f);
                openPen.DashStyle = DashStyle.Dash;
                graphics.DrawRectangle(openPen, cell.X, cell.Y, cell.Width, cell.Height);
                return;
            }

            Color doorColor = state == DoorVisualState.ClosingWarning
                ? Color.FromArgb(255, 231, 98)
                : Color.FromArgb(255, 195, 45);
            using SolidBrush doorBrush = new SolidBrush(doorColor);
            using Pen doorPen = new Pen(Color.FromArgb(200, 137, 5), CELL_BITMAP_SIZE * 0.06f);
            if (state == DoorVisualState.ClosingWarning)
            {
                doorPen.DashStyle = DashStyle.Dot;
            }

            graphics.FillRectangle(doorBrush, cell);
            graphics.DrawRectangle(doorPen, cell.X, cell.Y, cell.Width, cell.Height);
            for (int stripe = -1; stripe < 4; stripe++)
            {
                float stripeX = cell.Left + stripe * cell.Width * 0.38f;
                graphics.DrawLine(
                    doorPen,
                    stripeX,
                    cell.Bottom,
                    stripeX + cell.Width * 0.65f,
                    cell.Top);
            }
        }

        private void DrawObject(Graphics graphics, BoardObjectKind kind)
        {
            RectangleF cell = GetCellRectangle(0.08f);
            switch (kind)
            {
                case BoardObjectKind.OrdinaryFood:
                    DrawStrawberry(graphics, cell);
                    break;
                case BoardObjectKind.SpecialFood:
                    DrawBlueberries(graphics, cell);
                    break;
                case BoardObjectKind.Shield:
                    DrawShield(graphics, cell);
                    break;
                case BoardObjectKind.Fire:
                    DrawFire(graphics, cell);
                    break;
                case BoardObjectKind.Bomb:
                    DrawBomb(graphics, cell);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "不支援的遊戲物件圖案。");
            }
        }

        private void DrawStrawberry(Graphics graphics, RectangleF cell)
        {
            RectangleF berry = new RectangleF(
                cell.Left + cell.Width * 0.23f,
                cell.Top + cell.Height * 0.25f,
                cell.Width * 0.54f,
                cell.Height * 0.58f);
            using SolidBrush berryBrush = new SolidBrush(Color.FromArgb(231, 69, 78));
            using Pen outlinePen = new Pen(Color.FromArgb(164, 43, 52), cell.Width * 0.04f);
            graphics.FillEllipse(berryBrush, berry);
            graphics.DrawEllipse(outlinePen, berry);

            PointF[] leaves =
            {
                new PointF(cell.Left + cell.Width * 0.28f, cell.Top + cell.Height * 0.34f),
                new PointF(cell.Left + cell.Width * 0.5f, cell.Top + cell.Height * 0.12f),
                new PointF(cell.Left + cell.Width * 0.72f, cell.Top + cell.Height * 0.34f),
                new PointF(cell.Left + cell.Width * 0.5f, cell.Top + cell.Height * 0.29f)
            };
            using SolidBrush leafBrush = new SolidBrush(Color.FromArgb(48, 151, 72));
            graphics.FillPolygon(leafBrush, leaves);
            using SolidBrush seedBrush = new SolidBrush(Color.FromArgb(255, 231, 126));
            float seedSize = cell.Width * 0.045f;
            graphics.FillEllipse(seedBrush, berry.Left + berry.Width * 0.28f, berry.Top + berry.Height * 0.38f, seedSize, seedSize);
            graphics.FillEllipse(seedBrush, berry.Left + berry.Width * 0.62f, berry.Top + berry.Height * 0.52f, seedSize, seedSize);
        }

        private void DrawBlueberries(Graphics graphics, RectangleF cell)
        {
            using SolidBrush berryBrush = new SolidBrush(Color.FromArgb(70, 91, 184));
            using Pen outlinePen = new Pen(Color.FromArgb(37, 55, 127), cell.Width * 0.035f);
            float berrySize = cell.Width * 0.42f;
            RectangleF leftBerry = new RectangleF(
                cell.Left + cell.Width * 0.16f,
                cell.Top + cell.Height * 0.38f,
                berrySize,
                berrySize);
            RectangleF rightBerry = new RectangleF(
                cell.Left + cell.Width * 0.43f,
                cell.Top + cell.Height * 0.28f,
                berrySize,
                berrySize);
            graphics.FillEllipse(berryBrush, leftBerry);
            graphics.DrawEllipse(outlinePen, leftBerry);
            graphics.FillEllipse(berryBrush, rightBerry);
            graphics.DrawEllipse(outlinePen, rightBerry);
            using SolidBrush crownBrush = new SolidBrush(Color.FromArgb(190, 211, 240));
            graphics.FillEllipse(
                crownBrush,
                rightBerry.Left + rightBerry.Width * 0.36f,
                rightBerry.Top + rightBerry.Height * 0.12f,
                rightBerry.Width * 0.25f,
                rightBerry.Height * 0.15f);
        }

        private void DrawShield(Graphics graphics, RectangleF cell)
        {
            PointF[] shield =
            {
                new PointF(cell.Left + cell.Width * 0.5f, cell.Top + cell.Height * 0.1f),
                new PointF(cell.Left + cell.Width * 0.78f, cell.Top + cell.Height * 0.22f),
                new PointF(cell.Left + cell.Width * 0.73f, cell.Top + cell.Height * 0.64f),
                new PointF(cell.Left + cell.Width * 0.5f, cell.Top + cell.Height * 0.88f),
                new PointF(cell.Left + cell.Width * 0.27f, cell.Top + cell.Height * 0.64f),
                new PointF(cell.Left + cell.Width * 0.22f, cell.Top + cell.Height * 0.22f)
            };
            using SolidBrush shieldBrush = new SolidBrush(Color.FromArgb(93, 178, 214));
            using Pen outlinePen = new Pen(Color.FromArgb(21, 82, 117), cell.Width * 0.05f);
            graphics.FillPolygon(shieldBrush, shield);
            graphics.DrawPolygon(outlinePen, shield);
            graphics.DrawLine(
                outlinePen,
                cell.Left + cell.Width * 0.5f,
                cell.Top + cell.Height * 0.18f,
                cell.Left + cell.Width * 0.5f,
                cell.Top + cell.Height * 0.76f);
        }

        private void DrawFire(Graphics graphics, RectangleF cell)
        {
            using GraphicsPath outerPath = new GraphicsPath();
            outerPath.AddBezier(
                cell.Left + cell.Width * 0.5f,
                cell.Top + cell.Height * 0.08f,
                cell.Left + cell.Width * 0.8f,
                cell.Top + cell.Height * 0.42f,
                cell.Left + cell.Width * 0.84f,
                cell.Top + cell.Height * 0.76f,
                cell.Left + cell.Width * 0.5f,
                cell.Top + cell.Height * 0.9f);
            outerPath.AddBezier(
                cell.Left + cell.Width * 0.5f,
                cell.Top + cell.Height * 0.9f,
                cell.Left + cell.Width * 0.16f,
                cell.Top + cell.Height * 0.76f,
                cell.Left + cell.Width * 0.24f,
                cell.Top + cell.Height * 0.38f,
                cell.Left + cell.Width * 0.5f,
                cell.Top + cell.Height * 0.08f);
            outerPath.CloseFigure();
            using SolidBrush outerBrush = new SolidBrush(Color.FromArgb(240, 90, 48));
            graphics.FillPath(outerBrush, outerPath);
            using SolidBrush innerBrush = new SolidBrush(Color.FromArgb(255, 204, 52));
            graphics.FillEllipse(
                innerBrush,
                cell.Left + cell.Width * 0.37f,
                cell.Top + cell.Height * 0.5f,
                cell.Width * 0.27f,
                cell.Height * 0.32f);
        }

        private void DrawBomb(Graphics graphics, RectangleF cell)
        {
            RectangleF bomb = new RectangleF(
                cell.Left + cell.Width * 0.2f,
                cell.Top + cell.Height * 0.3f,
                cell.Width * 0.58f,
                cell.Height * 0.58f);
            using SolidBrush bombBrush = new SolidBrush(Color.FromArgb(31, 42, 50));
            using Pen outlinePen = new Pen(Color.FromArgb(8, 18, 25), cell.Width * 0.04f);
            graphics.FillEllipse(bombBrush, bomb);
            graphics.DrawEllipse(outlinePen, bomb);
            using Pen fusePen = new Pen(Color.FromArgb(125, 82, 43), cell.Width * 0.055f);
            graphics.DrawArc(
                fusePen,
                cell.Left + cell.Width * 0.52f,
                cell.Top + cell.Height * 0.12f,
                cell.Width * 0.3f,
                cell.Height * 0.34f,
                170,
                120);
            using SolidBrush sparkBrush = new SolidBrush(Color.FromArgb(255, 185, 39));
            graphics.FillEllipse(
                sparkBrush,
                cell.Left + cell.Width * 0.76f,
                cell.Top + cell.Height * 0.08f,
                cell.Width * 0.12f,
                cell.Height * 0.12f);
        }

        private void DrawSnakeSegment(Graphics graphics, bool isHead)
        {
            RectangleF cell = GetCellRectangle(isHead ? 0.09f : 0.14f);
            using GraphicsPath path = CreateRoundedRectangle(cell, CELL_BITMAP_SIZE * 0.2f);
            using LinearGradientBrush snakeBrush = new LinearGradientBrush(
                cell,
                Color.FromArgb(163, 236, 80),
                Color.FromArgb(32, 168, 72),
                45f);
            using Pen borderPen = new Pen(Color.FromArgb(20, 131, 62), CELL_BITMAP_SIZE * 0.045f);
            graphics.FillPath(snakeBrush, path);
            graphics.DrawPath(borderPen, path);
            if (isHead)
            {
                DrawSnakeFace(graphics, cell);
            }
        }

        private void DrawSnakeFace(Graphics graphics, RectangleF head)
        {
            float eyeSize = CELL_BITMAP_SIZE * 0.1f;
            float eyeY = head.Top + head.Height * 0.25f;
            float firstEyeX = head.Left + head.Width * 0.3f;
            float secondEyeX = head.Left + head.Width * 0.62f;
            using SolidBrush eyeWhiteBrush = new SolidBrush(Color.White);
            using SolidBrush pupilBrush = new SolidBrush(Color.FromArgb(23, 61, 104));
            graphics.FillEllipse(eyeWhiteBrush, firstEyeX, eyeY, eyeSize * 1.35f, eyeSize * 1.35f);
            graphics.FillEllipse(eyeWhiteBrush, secondEyeX, eyeY, eyeSize * 1.35f, eyeSize * 1.35f);
            graphics.FillEllipse(pupilBrush, firstEyeX + eyeSize * 0.42f, eyeY + eyeSize * 0.4f, eyeSize * 0.55f, eyeSize * 0.55f);
            graphics.FillEllipse(pupilBrush, secondEyeX + eyeSize * 0.42f, eyeY + eyeSize * 0.4f, eyeSize * 0.55f, eyeSize * 0.55f);
        }

        private RectangleF GetCellRectangle(float insetRatio)
        {
            float inset = CELL_BITMAP_SIZE * insetRatio;
            return new RectangleF(
                inset,
                inset,
                CELL_BITMAP_SIZE - inset * 2,
                CELL_BITMAP_SIZE - inset * 2);
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

        private void OnProgressPaint(object? sender, PaintEventArgs eventArgs)
        {
            Graphics graphics = eventArgs.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF bounds = new RectangleF(1, 1, Math.Max(1, _progressSurface.Width - 3), Math.Max(1, _progressSurface.Height - 3));
            using SolidBrush trackBrush = new SolidBrush(Color.FromArgb(215, 221, 225));
            graphics.FillRectangle(trackBrush, bounds);
            if (_progressPercent <= 0)
            {
                return;
            }

            RectangleF fill = new RectangleF(bounds.X, bounds.Y, bounds.Width * Math.Clamp(_progressPercent, 0, 100) / 100f, bounds.Height);
            using SolidBrush fillBrush = new SolidBrush(Color.FromArgb(40, 183, 71));
            graphics.FillRectangle(fillBrush, fill);
        }

        private string GetRuleGlyph(string rule)
        {
            if (rule.Contains("普通食物", StringComparison.Ordinal))
            {
                return "●";
            }

            if (rule.Contains("特殊食物", StringComparison.Ordinal))
            {
                return "●●";
            }

            if (rule.Contains("護盾", StringComparison.Ordinal))
            {
                return "◆";
            }

            if (rule.Contains("火焰", StringComparison.Ordinal))
            {
                return "▲";
            }

            if (rule.Contains("炸彈", StringComparison.Ordinal))
            {
                return "●";
            }

            return "•";
        }

        private string GetDoorStateText(DoorVisualState state)
        {
            return state switch
            {
                DoorVisualState.Open => "開啟",
                DoorVisualState.ClosingWarning => "即將關閉",
                DoorVisualState.CloseDeferred => "等待蛇離開",
                DoorVisualState.Closed => "關閉",
                _ => state.ToString()
            };
        }

        private string GetDeathCauseText(string deathCause)
        {
            return deathCause switch
            {
                "SelfCollision" => "撞到自己的身體",
                "WallCollision" or "BoundaryCollision" => "撞到牆或邊界",
                "ClosedDoorCollision" => "撞到關閉的門",
                "Bomb" => "碰到炸彈",
                "FatalFire" => "火焰使蛇身歸零",
                _ => deathCause
            };
        }

        private Label CreateLabel(float size, FontStyle style, int height, ContentAlignment alignment)
        {
            return new Label
            {
                AutoSize = false,
                Height = height,
                TextAlign = alignment,
                Font = new Font("Microsoft JhengHei", size, style, GraphicsUnit.Point),
                Margin = new Padding(0),
                Width = 360
            };
        }

        private Panel CreateCardPanel()
        {
            Panel panel = new Panel
            {
                Width = 360,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 14)
            };
            panel.Paint += OnCardPaint;
            return panel;
        }

        private Button CreateActionButton(string text)
        {
            return new Button
            {
                Text = text,
                Width = 360,
                Height = 58,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(54, 158, 75),
                ForeColor = Color.White,
                Font = new Font("Microsoft JhengHei", 18, FontStyle.Bold, GraphicsUnit.Point),
                Margin = new Padding(0, 0, 0, 12)
            };
        }

        private void OnCardPaint(object? sender, PaintEventArgs eventArgs)
        {
            if (sender is not Panel panel)
            {
                return;
            }

            using Pen borderPen = new Pen(Color.FromArgb(205, 218, 210), 2);
            eventArgs.Graphics.DrawRectangle(borderPen, 1, 1, Math.Max(1, panel.Width - 3), Math.Max(1, panel.Height - 3));
        }

        private void DisposeRenderingResources()
        {
            if (_boardCellImages is null)
            {
                return;
            }

            for (int y = 0; y < BOARD_SIZE; y++)
            {
                for (int x = 0; x < BOARD_SIZE; x++)
                {
                    _boardCellImages[x, y]?.Dispose();
                }
            }
        }
    }
}

