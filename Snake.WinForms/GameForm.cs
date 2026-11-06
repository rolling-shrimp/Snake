using System.Diagnostics;
using Snake.UseCases;

namespace Snake.WinForms;

public class GameForm : Form, IGameView
{
    private TableLayoutPanel _mainLayout;
    private GameBoardControl _boardControl;
    private FlowLayoutPanel _sidebarPanel;
    private Label _levelLabel;
    private Panel _statusPanel;
    private Label _lengthLabel;
    private Panel _shieldPanel;
    private Label _shieldLabel;
    private GameProgressControl _progressBar;
    private Label _progressLabel;
    private Panel _rulesPanel;
    private FlowLayoutPanel _rulesList;
    private Panel _resultPanel;
    private Label _resultTitleLabel;
    private Label _resultMessageLabel;
    private Button _startButton;
    private Button _replayButton;
    private Label _errorLabel;
    private System.Windows.Forms.Timer _gameTimer;
    private Stopwatch _levelClock;
    private List<Label> _ruleLabels;

    public GameForm()
    {
        Text = "貪吃蛇";
        ClientSize = new Size(1280, 860);
        MinimumSize = new Size(1000, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(251, 246, 232);
        Font = new Font("Microsoft JhengHei", 11, FontStyle.Regular, GraphicsUnit.Point);
        KeyPreview = true;
        AutoScaleMode = AutoScaleMode.Dpi;

        _mainLayout = new TableLayoutPanel();
        _boardControl = new GameBoardControl();
        _sidebarPanel = new FlowLayoutPanel();
        _levelLabel = CreateLabel(34, FontStyle.Bold, 68, ContentAlignment.MiddleCenter);
        _statusPanel = CreateCardPanel();
        _lengthLabel = CreateLabel(34, FontStyle.Bold, 52, ContentAlignment.MiddleLeft);
        _shieldPanel = CreateCardPanel();
        _shieldLabel = CreateLabel(21, FontStyle.Bold, 48, ContentAlignment.MiddleCenter);
        _progressBar = new GameProgressControl();
        _progressLabel = CreateLabel(12, FontStyle.Bold, 32, ContentAlignment.MiddleLeft);
        _rulesPanel = CreateCardPanel();
        _rulesList = new FlowLayoutPanel();
        _resultPanel = CreateCardPanel();
        _resultTitleLabel = CreateLabel(22, FontStyle.Bold, 42, ContentAlignment.MiddleCenter);
        _resultMessageLabel = CreateLabel(12, FontStyle.Bold, 68, ContentAlignment.TopCenter);
        _startButton = CreateActionButton("開始");
        _replayButton = CreateActionButton("重玩");
        _errorLabel = CreateLabel(11, FontStyle.Bold, 52, ContentAlignment.MiddleCenter);
        _gameTimer = new System.Windows.Forms.Timer();
        _levelClock = new Stopwatch();
        _ruleLabels = new List<Label>();

        _gameTimer.Interval = 250;
        _gameTimer.Tick += OnGameTimerTick;
        BuildLayout();
    }

    public event EventHandler? StartRequested;
    public event EventHandler? ReplayRequested;
    public event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;
    public event EventHandler<GameTickEventArgs>? TickRequested;

    public void ApplyRuntimeDirective(RuntimeDirective runtimeDirective)
    {
        if (runtimeDirective == RuntimeDirective.StartOrReset)
        {
            _gameTimer.Stop();
            _levelClock.Restart();
            _gameTimer.Start();
        }
        else if (runtimeDirective == RuntimeDirective.Stop)
        {
            _gameTimer.Stop();
            _levelClock.Stop();
        }
    }

    public void RenderGameScreen(GameScreenOutput screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        GameViewModel viewModel = CreateViewModel(screen);
        SuspendLayout();
        try
        {
            _errorLabel.Visible = false;
            _boardControl.Render(viewModel.Board);
            _boardControl.Visible = viewModel.Board.IsVisible;
            SetBoardLayout(viewModel.Board.IsVisible);
            RenderStatus(viewModel.Status);
            RenderRules(viewModel);
            RenderResult(viewModel.ResultCard);

            _startButton.Visible = viewModel.IsStartButtonVisible;
            _startButton.Enabled = viewModel.IsStartButtonVisible;
            _replayButton.Visible = viewModel.IsReplayButtonVisible;
            _replayButton.Enabled = viewModel.IsReplayButtonVisible;

            ResizeSidebarChildren();
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    public void ShowFailure(string message)
    {
        _errorLabel.Text = message;
        _errorLabel.Visible = true;
        _startButton.Enabled = false;
        _replayButton.Enabled = false;
        ResizeSidebarChildren();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gameTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        Direction? direction = (keyData & Keys.KeyCode) switch
        {
            Keys.Up => Direction.Up,
            Keys.Down => Direction.Down,
            Keys.Left => Direction.Left,
            Keys.Right => Direction.Right,
            _ => null
        };
        if (direction is null)
        {
            return base.ProcessCmdKey(ref message, keyData);
        }

        DirectionRequested?.Invoke(this, new DirectionRequestedEventArgs(direction.Value));
        return true;
    }

    private GameViewModel CreateViewModel(GameScreenOutput screen)
    {
        bool isBoardVisible = screen.ScreenKind != GameScreenKind.GameOver;
        int rows = screen.BoardCells.Count == 0
            ? 10
            : screen.BoardCells.Max(cell => cell.Position.Y) + 1;
        int columns = screen.BoardCells.Count == 0
            ? 10
            : screen.BoardCells.Max(cell => cell.Position.X) + 1;
        GameBoardViewModel board = new GameBoardViewModel(
            isBoardVisible,
            rows,
            columns,
            screen.SnakeBody.Select(ToGridCell).ToList(),
            screen.WallCells.Select(ToGridCell).ToList(),
            screen.Doors.Select(door => new DoorViewModel(
                Guid.Parse(door.DoorId),
                ToGridCell(door.Position),
                door.State.ToString())).ToList(),
            screen.Objects.Select(ToBoardObject).ToList());
        LevelStatusOutput levelStatus = screen.LevelStatus;
        int? shieldRemainingCharges = levelStatus.LevelNumber == 1
            ? null
            : levelStatus.RemainingShieldCharges;
        GameStatusViewModel status = new GameStatusViewModel(
            GetLevelName(levelStatus.LevelNumber),
            levelStatus.CurrentLength,
            levelStatus.TargetLength,
            levelStatus.ProgressPercentage,
            shieldRemainingCharges);
        return new GameViewModel(
            GetScreenId(screen.ScreenKind),
            board,
            status,
            screen.RuleHints.Select(rule => new RuleHintViewModel("RULE", rule)).ToList(),
            screen.AvailableActions.Contains(GameAction.Start),
            screen.AvailableActions.Contains(GameAction.Restart),
            screen.AvailableActions.Contains(GameAction.ChangeDirection),
            250,
            ToResultCard(screen.ResultCard));
    }

    private BoardObjectViewModel ToBoardObject(BoardObjectOutput gameObject)
    {
        return new BoardObjectViewModel(
            gameObject.Kind,
            gameObject.ObjectId,
            ToGridCell(gameObject.Position));
    }

    private GameResultCardViewModel? ToResultCard(ResultCardOutput? resultCard)
    {
        if (resultCard is null)
        {
            return null;
        }

        string title = resultCard.Outcome switch
        {
            "Failed" => "Game Over",
            "Passed" when resultCard.LevelNumber == 3 => "闖關成功",
            "Passed" => $"第 {resultCard.LevelNumber} 關過關",
            _ => resultCard.Outcome
        };
        string message = resultCard.Outcome == "Failed"
            ? "遊戲結束"
            : resultCard.LevelNumber == 3
                ? "三關全部完成"
                : "關卡通過";
        string detail = string.IsNullOrWhiteSpace(resultCard.DeathCause)
            ? $"最後蛇長：{resultCard.FinalLength} 格"
            : GetDeathCauseText(resultCard.DeathCause);
        return new GameResultCardViewModel(
            title,
            message,
            resultCard.LevelNumber,
            resultCard.FinalLength,
            detail);
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

    private string GetScreenId(GameScreenKind screenKind)
    {
        return screenKind switch
        {
            GameScreenKind.FirstLevelReady => "first-level-ready",
            GameScreenKind.FirstLevelPlaying => "first-level-playing",
            GameScreenKind.SecondLevelReady => "second-level-ready",
            GameScreenKind.SecondLevelPlaying => "second-level-playing",
            GameScreenKind.ThirdLevelReady => "third-level-ready",
            GameScreenKind.ThirdLevelPlaying => "third-level-playing",
            GameScreenKind.GameOver => "game-over",
            GameScreenKind.GameCompleted => "game-completed",
            _ => throw new ArgumentOutOfRangeException(
                nameof(screenKind),
                screenKind,
                "不支援的畫面種類。")
        };
    }

    private string GetLevelName(int levelNumber)
    {
        return levelNumber switch
        {
            1 => "第一關",
            2 => "第二關",
            3 => "第三關",
            _ => $"第 {levelNumber} 關"
        };
    }

    private GridCellViewModel ToGridCell(GridPositionDTO position)
    {
        return new GridCellViewModel(position.X, position.Y);
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

        _boardControl.Dock = DockStyle.Fill;
        _boardControl.Margin = new Padding(0, 0, 20, 0);

        _sidebarPanel.Dock = DockStyle.Fill;
        _sidebarPanel.FlowDirection = FlowDirection.TopDown;
        _sidebarPanel.WrapContents = false;
        _sidebarPanel.AutoScroll = true;
        _sidebarPanel.Padding = new Padding(12, 4, 12, 12);
        _sidebarPanel.BackColor = Color.FromArgb(255, 253, 242);
        _sidebarPanel.AccessibleName = "關卡資訊";
        _sidebarPanel.Resize += OnSidebarResize;

        _levelLabel.BackColor = Color.FromArgb(211, 247, 190);
        _levelLabel.ForeColor = Color.FromArgb(13, 58, 107);
        _levelLabel.Margin = new Padding(0, 0, 0, 14);
        _levelLabel.AccessibleName = "目前關卡";

        BuildStatusCard();
        BuildRulesCard();
        BuildResultCard();

        _startButton.Click += OnStartButtonClick;
        _startButton.AccessibleName = "開始目前關卡";
        _replayButton.Click += OnReplayButtonClick;
        _replayButton.AccessibleName = "重新開始遊戲";
        _errorLabel.ForeColor = Color.FromArgb(170, 48, 48);
        _errorLabel.BackColor = Color.FromArgb(255, 235, 230);
        _errorLabel.Visible = false;

        _sidebarPanel.Controls.Add(_resultPanel);
        _sidebarPanel.Controls.Add(_levelLabel);
        _sidebarPanel.Controls.Add(_statusPanel);
        _sidebarPanel.Controls.Add(_rulesPanel);
        _sidebarPanel.Controls.Add(_startButton);
        _sidebarPanel.Controls.Add(_replayButton);
        _sidebarPanel.Controls.Add(_errorLabel);
        _mainLayout.Controls.Add(_boardControl, 0, 0);
        _mainLayout.Controls.Add(_sidebarPanel, 1, 0);
        Controls.Add(_mainLayout);
    }

    private void BuildStatusCard()
    {
        _statusPanel.Height = 174;
        _statusPanel.Padding = new Padding(18, 12, 18, 12);
        _statusPanel.Margin = new Padding(0, 0, 0, 14);
        _statusPanel.AccessibleName = "關卡狀態";

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

        Panel lengthPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
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

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Margin = new Padding(0, 3, 0, 3);
        _progressLabel.Dock = DockStyle.Fill;
        _progressLabel.ForeColor = Color.FromArgb(38, 76, 115);

        statusLayout.Controls.Add(lengthPanel, 0, 0);
        statusLayout.Controls.Add(_shieldPanel, 1, 0);
        statusLayout.Controls.Add(_progressBar, 0, 1);
        statusLayout.SetColumnSpan(_progressBar, 2);
        statusLayout.Controls.Add(_progressLabel, 0, 2);
        statusLayout.SetColumnSpan(_progressLabel, 2);
        _statusPanel.Controls.Add(statusLayout);
    }

    private void BuildRulesCard()
    {
        _rulesPanel.Padding = new Padding(16, 12, 16, 12);
        _rulesPanel.Margin = new Padding(0, 0, 0, 14);
        _rulesPanel.AccessibleName = "規則提示";

        Label rulesTitle = CreateLabel(16, FontStyle.Bold, 36, ContentAlignment.MiddleLeft);
        rulesTitle.Text = "規則提示";
        rulesTitle.Dock = DockStyle.Top;
        rulesTitle.ForeColor = Color.FromArgb(33, 71, 110);

        _rulesList.Dock = DockStyle.Fill;
        _rulesList.FlowDirection = FlowDirection.TopDown;
        _rulesList.WrapContents = false;
        _rulesList.BackColor = Color.Transparent;
        _rulesPanel.Controls.Add(_rulesList);
        _rulesPanel.Controls.Add(rulesTitle);
    }

    private void BuildResultCard()
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

    private void RenderStatus(GameStatusViewModel? status)
    {
        _statusPanel.Visible = status is not null;
        _levelLabel.Visible = status is not null;
        if (status is null)
        {
            return;
        }

        _levelLabel.Text = status.LevelName;
        _lengthLabel.Text = $"{status.CurrentLength} / {status.TargetLength}";
        _shieldPanel.Visible = status.ShieldRemainingCharges is not null;
        _shieldLabel.Text = $"{status.ShieldRemainingCharges ?? 0} 次";
        _progressBar.SetProgress(status.ProgressPercent);
        _progressLabel.Text = $"過關進度 {status.ProgressPercent}%     目標 {status.TargetLength} 格";
    }

    private void RenderRules(GameViewModel viewModel)
    {
        _rulesList.SuspendLayout();
        try
        {
            List<string> visibleRules = viewModel.RuleHints
                .Select(ruleHint => $"{GetRuleGlyph(ruleHint.Text)}   {ruleHint.Text}")
                .ToList();
            if (viewModel.ScreenId.StartsWith("third-level-", StringComparison.Ordinal)
                && viewModel.Board.Doors.Count > 0)
            {
                string doorState = GetDoorStateText(viewModel.Board.Doors[0].State);
                visibleRules.Add($"[門]   門目前：{doorState}");
            }

            while (_ruleLabels.Count < visibleRules.Count)
            {
                Label ruleLabel = CreateRuleLabel(string.Empty, string.Empty);
                _ruleLabels.Add(ruleLabel);
                _rulesList.Controls.Add(ruleLabel);
            }

            for (int index = 0; index < _ruleLabels.Count; index++)
            {
                bool isVisible = index < visibleRules.Count;
                _ruleLabels[index].Visible = isVisible;
                if (isVisible)
                {
                    _ruleLabels[index].Text = visibleRules[index];
                    _ruleLabels[index].AccessibleName = visibleRules[index];
                }
            }

            _rulesPanel.Visible = visibleRules.Count > 0;
            _rulesPanel.Height = 60 + visibleRules.Count * 43;
        }
        finally
        {
            _rulesList.ResumeLayout(true);
        }
    }

    private void RenderResult(GameResultCardViewModel? resultCard)
    {
        _resultPanel.Visible = resultCard is not null;
        if (resultCard is null)
        {
            return;
        }

        _resultTitleLabel.Text = resultCard.Title;
        string levelText = resultCard.LevelNumber is null
            ? string.Empty
            : $"第 {resultCard.LevelNumber} 關 · ";
        string detail = string.IsNullOrWhiteSpace(resultCard.Detail)
            ? string.Empty
            : $"{Environment.NewLine}{resultCard.Detail}";
        _resultMessageLabel.Text =
            $"{resultCard.Message}{Environment.NewLine}{levelText}最後蛇長 {resultCard.FinalLength}{detail}";
        _resultPanel.AccessibleDescription = _resultMessageLabel.Text;
    }

    private Label CreateRuleLabel(string glyph, string text)
    {
        return new Label
        {
            AutoSize = false,
            Height = 42,
            Margin = new Padding(0),
            Font = new Font("Microsoft JhengHei", 10.5f, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(30, 66, 101),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = $"{glyph}   {text}",
            AccessibleName = text
        };
    }

    private Label CreateLabel(
        float fontSize,
        FontStyle style,
        int height,
        ContentAlignment alignment)
    {
        return new Label
        {
            AutoSize = false,
            Height = height,
            Font = new Font("Microsoft JhengHei", fontSize, style, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(43, 70, 54),
            TextAlign = alignment,
            BackColor = Color.Transparent
        };
    }

    private Panel CreateCardPanel()
    {
        Panel panel = new Panel
        {
            BackColor = Color.FromArgb(255, 253, 245)
        };
        panel.Paint += OnCardPaint;
        return panel;
    }

    private Button CreateActionButton(string text)
    {
        Button button = new Button
        {
            Text = text,
            Height = 58,
            Margin = new Padding(0, 0, 0, 14),
            Font = new Font("Microsoft JhengHei", 18, FontStyle.Bold, GraphicsUnit.Point),
            BackColor = Color.FromArgb(43, 171, 76),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TabStop = true,
            Visible = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 190, 86);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(21, 130, 58);
        return button;
    }

    private void SetBoardLayout(bool isBoardVisible)
    {
        _mainLayout.ColumnStyles[0].Width = isBoardVisible ? 67 : 0;
        _mainLayout.ColumnStyles[1].Width = isBoardVisible ? 33 : 100;
        _boardControl.Margin = isBoardVisible
            ? new Padding(0, 0, 20, 0)
            : new Padding(0);
    }

    private void ResizeSidebarChildren()
    {
        int availableWidth = Math.Max(220, _sidebarPanel.ClientSize.Width - 32);
        int controlWidth = Math.Min(560, availableWidth);
        int sideMargin = Math.Max(0, (availableWidth - controlWidth) / 2);
        foreach (Control control in _sidebarPanel.Controls)
        {
            control.Width = controlWidth;
            control.Margin = new Padding(sideMargin, control.Margin.Top, sideMargin, control.Margin.Bottom);
        }

        foreach (Control control in _rulesList.Controls)
        {
            control.Width = Math.Max(160, _rulesList.ClientSize.Width - 4);
        }
    }

    private string GetRuleGlyph(string ruleText)
    {
        if (ruleText.Contains("普通食物", StringComparison.Ordinal))
        {
            return "[草莓]";
        }

        if (ruleText.Contains("特殊食物", StringComparison.Ordinal))
        {
            return "[藍莓]";
        }

        if (ruleText.Contains("護盾", StringComparison.Ordinal))
        {
            return "[護盾]";
        }

        if (ruleText.Contains("火焰", StringComparison.Ordinal))
        {
            return "[火焰]";
        }

        if (ruleText.Contains("炸彈", StringComparison.Ordinal))
        {
            return "[炸彈]";
        }

        if (ruleText.Contains("穿越", StringComparison.Ordinal))
        {
            return "[穿越]";
        }

        if (ruleText.Contains("牆", StringComparison.Ordinal))
        {
            return "[牆]";
        }

        return "[注意]";
    }

    private string GetDoorStateText(string state)
    {
        return state switch
        {
            "Open" => "開啟",
            "ClosingWarning" => "即將關閉",
            "CloseDeferred" => "蛇身通過中，延後關閉",
            "Closed" => "關閉",
            _ => state
        };
    }

    private void OnCardPaint(object? sender, PaintEventArgs eventArgs)
    {
        if (sender is not Panel panel)
        {
            return;
        }

        using Pen borderPen = new Pen(Color.FromArgb(243, 223, 160), 2);
        eventArgs.Graphics.DrawRectangle(
            borderPen,
            1,
            1,
            Math.Max(0, panel.ClientSize.Width - 3),
            Math.Max(0, panel.ClientSize.Height - 3));
    }

    private void OnSidebarResize(object? sender, EventArgs eventArgs)
    {
        ResizeSidebarChildren();
    }

    private void OnStartButtonClick(object? sender, EventArgs eventArgs)
    {
        StartRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnReplayButtonClick(object? sender, EventArgs eventArgs)
    {
        ReplayRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnGameTimerTick(object? sender, EventArgs eventArgs)
    {
        TickRequested?.Invoke(this, new GameTickEventArgs(_levelClock.ElapsedMilliseconds));
    }

}
