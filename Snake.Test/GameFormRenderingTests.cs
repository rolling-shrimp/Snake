using System.Drawing;
using Snake.Entities;
using Snake.UseCases;
using Snake.WinForms;

namespace Snake.Test;

public class GameFormRenderingTests
{
    private StaTestRunner _staTestRunner = new StaTestRunner();

    [Fact]
    public void FirstLevelReady_RendersSpecifiedBoardStatusRulesAndStartButton()
    {
        _staTestRunner.Run(() =>
        {
            using GameForm form = new GameForm();
            SnakeGameSession session = new SnakeGameSession();
            SnakeGameApplicationService service = new SnakeGameApplicationService(
                session,
                new FirstLegalSpawnPositionSelector());
            GamePresenter presenter = new GamePresenter(form, service);
            presenter.Initialize();

            using Bitmap bitmap = Render(form);
            List<string> visibleText = GetVisibleText(form);

            Assert.Contains("第一關", visibleText);
            Assert.Contains("3 / 20", visibleText);
            Assert.Contains("過關進度 15%     目標 20 格", visibleText);
            Assert.Contains("普通食物 +1 格", string.Join("|", visibleText));
            Assert.Contains("開始", visibleText);
            Assert.DoesNotContain("重玩", visibleText);
            Assert.True(CountPixels(bitmap, Color.FromArgb(63, 126, 104)) > 100);
            Assert.True(CountPixels(bitmap, Color.FromArgb(255, 249, 223)) > 100);

            Button startButton = FindControls<Button>(form)
                .Single(button => button.Visible && button.Text == "開始");
            startButton.PerformClick();

            Assert.Equal(GamePhase.Playing, session.Load().Snapshot.Game.Phase);
            Assert.DoesNotContain(
                FindControls<Button>(form),
                button => button.Visible && button.Text == "開始");
            return true;
        });
    }

    [Fact]
    public void ThirdLevelPlaying_RendersWallsWarningDoorAndAllAllowedObjects()
    {
        _staTestRunner.Run(() =>
        {
            using GameForm form = new GameForm();
            GameScreenOutput screen = CreateThirdLevelPlayingScreen();
            form.RenderGameScreen(screen);

            using Bitmap bitmap = Render(form);
            string visibleText = string.Join("|", GetVisibleText(form));

            Assert.Contains("第三關", visibleText);
            Assert.Contains("0 次", visibleText);
            Assert.Contains("炸彈會觸發死亡事件", visibleText);
            Assert.Contains("門目前：即將關閉", visibleText);
            Assert.DoesNotContain("開始", visibleText);
            Assert.DoesNotContain("重玩", visibleText);
            Assert.True(CountPixels(bitmap, Color.FromArgb(113, 139, 158)) > 100);
            Assert.True(CountPixels(bitmap, Color.FromArgb(255, 231, 98)) > 100);
            return true;
        });
    }

    [Fact]
    public void GameOver_RendersResultWithoutBoardAndWithReplayButton()
    {
        _staTestRunner.Run(() =>
        {
            using GameForm form = new GameForm();
            form.RenderGameScreen(CreateGameOverScreen());

            using Bitmap bitmap = Render(form);
            List<string> visibleText = GetVisibleText(form);
            GameBoardControl boardControl = FindControls<GameBoardControl>(form).Single();

            Assert.Contains("Game Over", visibleText);
            Assert.Contains("重玩", visibleText);
            Assert.DoesNotContain("開始", visibleText);
            Assert.False(boardControl.Visible);
            return true;
        });
    }

    [Fact]
    public void RepeatedRender_PreservesStableControlIdentity()
    {
        _staTestRunner.Run(() =>
        {
            using GameForm form = new GameForm();
            GameScreenOutput screen = CreateThirdLevelPlayingScreen();
            form.RenderGameScreen(screen);
            GameBoardControl boardBefore = FindControls<GameBoardControl>(form).Single();
            List<Label> ruleLabelsBefore = FindControls<Label>(form)
                .Where(label => label.Visible && label.Text.Contains("食物", StringComparison.Ordinal))
                .ToList();

            form.RenderGameScreen(screen);

            Assert.Same(boardBefore, FindControls<GameBoardControl>(form).Single());
            List<Label> ruleLabelsAfter = FindControls<Label>(form)
                .Where(label => label.Visible && label.Text.Contains("食物", StringComparison.Ordinal))
                .ToList();
            Assert.Equal(ruleLabelsBefore.Count, ruleLabelsAfter.Count);
            for (int index = 0; index < ruleLabelsBefore.Count; index++)
            {
                Assert.Same(ruleLabelsBefore[index], ruleLabelsAfter[index]);
            }

            return true;
        });
    }

    [Theory]
    [InlineData(Keys.Up, Snake.UseCases.Direction.Up)]
    [InlineData(Keys.Down, Snake.UseCases.Direction.Down)]
    [InlineData(Keys.Left, Snake.UseCases.Direction.Left)]
    [InlineData(Keys.Right, Snake.UseCases.Direction.Right)]
    public void DirectionKeyMessage_WithFocusedButton_DispatchesExactlyOneDirectionRequest(
        Keys key,
        Snake.UseCases.Direction expectedDirection)
    {
        _staTestRunner.Run(() =>
        {
            using GameForm form = new GameForm();
            SnakeGameSession session = new SnakeGameSession();
            SnakeGameApplicationService service = new SnakeGameApplicationService(
                session,
                new FirstLegalSpawnPositionSelector());
            GamePresenter presenter = new GamePresenter(form, service);
            presenter.Initialize();
            List<Snake.UseCases.Direction> requestedDirections = new List<Snake.UseCases.Direction>();
            form.DirectionRequested += (_, eventArgs) => requestedDirections.Add(eventArgs.Direction);
            form.Show();
            Application.DoEvents();
            Button startButton = FindControls<Button>(form)
                .Single(button => button.Visible && button.Text == "開始");
            Assert.True(startButton.Focus());
            Message message = Message.Create(
                startButton.Handle,
                0x0100,
                new IntPtr((int)key),
                IntPtr.Zero);

            bool handled = startButton.PreProcessMessage(ref message);

            Assert.True(handled);
            Assert.Equal(new List<Snake.UseCases.Direction> { expectedDirection }, requestedDirections);
            return true;
        });
    }

    private GameScreenOutput CreateThirdLevelPlayingScreen()
    {
        return new GameScreenOutput(
            GameScreenKind.ThirdLevelPlaying,
            CreateBoardCells(),
            new List<GridPositionDTO>
            {
                new GridPositionDTO(3, 1),
                new GridPositionDTO(2, 1),
                new GridPositionDTO(1, 1)
            },
            new List<BoardObjectOutput>
            {
                CreateObject(BoardObjectKind.OrdinaryFood, 2, 3),
                CreateObject(BoardObjectKind.SpecialFood, 3, 3),
                CreateObject(BoardObjectKind.Shield, 4, 3),
                CreateObject(BoardObjectKind.Fire, 5, 3),
                CreateObject(BoardObjectKind.Bomb, 6, 3)
            },
            new List<GridPositionDTO>
            {
                new GridPositionDTO(0, 0),
                new GridPositionDTO(5, 4)
            },
            new List<DoorOutput>
            {
                new DoorOutput(
                    Guid.NewGuid().ToString(),
                    new GridPositionDTO(5, 5),
                    DoorVisualState.ClosingWarning)
            },
            new LevelStatusOutput(3, 3, 50, 6, 0),
            new List<string>
            {
                "普通食物 +1 格",
                "特殊食物 +2 格",
                "護盾增加 1 次保命",
                "火焰 -2 格",
                "炸彈會觸發死亡事件",
                "第三關不能穿牆"
            },
            null,
            new List<GameAction> { GameAction.ChangeDirection });
    }

    private GameScreenOutput CreateGameOverScreen()
    {
        return new GameScreenOutput(
            GameScreenKind.GameOver,
            CreateBoardCells(),
            new List<GridPositionDTO>(),
            new List<BoardObjectOutput>(),
            new List<GridPositionDTO>(),
            new List<DoorOutput>(),
            new LevelStatusOutput(2, 5, 20, 25, 0),
            new List<string>(),
            new ResultCardOutput("Failed", 2, 5, Snake.Entities.DeathCause.Bomb.ToString()),
            new List<GameAction> { GameAction.Restart });
    }

    private List<BoardCellOutput> CreateBoardCells()
    {
        List<BoardCellOutput> cells = new List<BoardCellOutput>();
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                cells.Add(new BoardCellOutput(
                    new GridPositionDTO(x, y),
                    BoardCellKind.Empty));
            }
        }

        return cells;
    }

    private BoardObjectOutput CreateObject(BoardObjectKind kind, int x, int y)
    {
        return new BoardObjectOutput(
            Guid.NewGuid().ToString(),
            kind,
            new GridPositionDTO(x, y));
    }

    private Bitmap Render(GameForm form)
    {
        form.Show();
        Application.DoEvents();
        form.PerformLayout();
        Bitmap bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        return bitmap;
    }

    private List<string> GetVisibleText(Control root)
    {
        List<string> text = new List<string>();
        foreach (Control control in FindControls<Control>(root))
        {
            if (control.Visible && !string.IsNullOrWhiteSpace(control.Text))
            {
                text.Add(control.Text);
            }
        }

        return text;
    }

    private List<TControl> FindControls<TControl>(Control root)
        where TControl : Control
    {
        List<TControl> controls = new List<TControl>();
        foreach (Control child in root.Controls)
        {
            if (child is TControl matchingControl)
            {
                controls.Add(matchingControl);
            }

            controls.AddRange(FindControls<TControl>(child));
        }

        return controls;
    }

    private int CountPixels(Bitmap bitmap, Color color)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y += 2)
        {
            for (int x = 0; x < bitmap.Width; x += 2)
            {
                if (bitmap.GetPixel(x, y).ToArgb() == color.ToArgb())
                {
                    count++;
                }
            }
        }

        return count;
    }
}
