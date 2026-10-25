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

        _gameTimer.Interval = 250;
        _gameTimer.Tick += OnGameTimerTick;
        KeyDown += OnGameKeyDown;
        BuildLayout();
    }

    public event EventHandler? StartRequested;
    public event EventHandler? ReplayRequested;
    public event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;
    public event EventHandler<GameTickEventArgs>? TickRequested;

    public void Render(GameViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
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

            _gameTimer.Stop();
            if (viewModel.IsMovementActive)
            {
                _gameTimer.Interval = Math.Max(1, viewModel.MovementIntervalMilliseconds);
                _gameTimer.Start();
            }

            ResizeSidebarChildren();
        }
        finally
        {
            ResumeLayout(true);
        }
    }

    public void ShowFailure(string message)
    {
        _gameTimer.Stop();
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
            _rulesList.Controls.Clear();
            foreach (RuleHintViewModel ruleHint in viewModel.RuleHints)
            {
                _rulesList.Controls.Add(CreateRuleLabel(
                    GetRuleGlyph(ruleHint.Code),
                    ruleHint.Text));
            }

            if (viewModel.ScreenId.StartsWith("third-level-", StringComparison.Ordinal)
                && viewModel.Board.Doors.Count > 0)
            {
                string doorState = GetDoorStateText(viewModel.Board.Doors[0].State);
                _rulesList.Controls.Add(CreateRuleLabel("[門]", $"門目前：{doorState}"));
            }

            _rulesPanel.Visible = _rulesList.Controls.Count > 0;
            _rulesPanel.Height = 60 + _rulesList.Controls.Count * 43;
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

    private string GetRuleGlyph(string code)
    {
        return code switch
        {
            "ORDINARY_FOOD_GROWTH" => "[草莓]",
            "SPECIAL_FOOD_GROWTH" => "[藍莓]",
            "SHIELD_RESCUE" => "[護盾]",
            "FIRE_SHRINK" => "[火焰]",
            "BOMB_FATAL" => "[炸彈]",
            "BOUNDARY_WRAP" => "[穿越]",
            "SELF_COLLISION_DEATH" => "[注意]",
            "WALL_BLOCKED" => "[牆]",
            _ => "•"
        };
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
        TickRequested?.Invoke(this, new GameTickEventArgs(_gameTimer.Interval));
    }

    private void OnGameKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        Direction? direction = eventArgs.KeyCode switch
        {
            Keys.Up or Keys.W => Direction.Up,
            Keys.Down or Keys.S => Direction.Down,
            Keys.Left or Keys.A => Direction.Left,
            Keys.Right or Keys.D => Direction.Right,
            _ => null
        };
        if (direction is null)
        {
            return;
        }

        DirectionRequested?.Invoke(this, new DirectionRequestedEventArgs(direction.Value));
        eventArgs.Handled = true;
        eventArgs.SuppressKeyPress = true;
    }
}
