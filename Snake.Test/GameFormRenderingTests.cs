using System.Drawing;
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
            GamePresenter presenter = new GamePresenter(form, session);
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
            SaveCaptureWhenRequested(bitmap, "snake-ui-first-ready.png");

            Button startButton = FindControls<Button>(form)
                .Single(button => button.Visible && button.Text == "開始");
            startButton.PerformClick();

            Assert.Equal(GameplayPhase.Playing, session.ReadGameplay().Phase);
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
            GameViewModel viewModel = new GameViewModel(
                "third-level-playing",
                new GameBoardViewModel(
                    true,
                    10,
                    10,
                    new List<GridCellViewModel>
                    {
                        new GridCellViewModel(3, 1),
                        new GridCellViewModel(2, 1),
                        new GridCellViewModel(1, 1)
                    },
                    new List<GridCellViewModel>
                    {
                        new GridCellViewModel(0, 0),
                        new GridCellViewModel(5, 4)
                    },
                    new List<DoorViewModel>
                    {
                        new DoorViewModel(
                            Guid.NewGuid(),
                            new GridCellViewModel(5, 5),
                            "ClosingWarning")
                    },
                    new List<BoardObjectViewModel>
                    {
                        CreateObject(GameObjectType.Strawberry, 2, 3),
                        CreateObject(GameObjectType.SpecialFood, 3, 3),
                        CreateObject(GameObjectType.Shield, 4, 3),
                        CreateObject(GameObjectType.Fire, 5, 3),
                        CreateObject(GameObjectType.Bomb, 6, 3)
                    }),
                new GameStatusViewModel("第三關", 3, 50, 6, 0),
                new List<RuleHintViewModel>
                {
                    new RuleHintViewModel("ORDINARY_FOOD_GROWTH", "普通食物 +1 格"),
                    new RuleHintViewModel("SPECIAL_FOOD_GROWTH", "特殊食物 +2 格"),
                    new RuleHintViewModel("SHIELD_RESCUE", "護盾 +1"),
                    new RuleHintViewModel("FIRE_SHRINK", "火焰 −2 格"),
                    new RuleHintViewModel("BOMB_FATAL", "炸彈致命"),
                    new RuleHintViewModel("WALL_BLOCKED", "不能穿牆")
                },
                false,
                false,
                true,
                250,
                null);
            form.Render(viewModel);

            using Bitmap bitmap = Render(form);
            string visibleText = string.Join("|", GetVisibleText(form));

            Assert.Contains("第三關", visibleText);
            Assert.Contains("0 次", visibleText);
            Assert.Contains("炸彈致命", visibleText);
            Assert.Contains("門目前：即將關閉", visibleText);
            Assert.DoesNotContain("開始", visibleText);
            Assert.DoesNotContain("重玩", visibleText);
            Assert.True(CountPixels(bitmap, Color.FromArgb(113, 139, 158)) > 100);
            Assert.True(CountPixels(bitmap, Color.FromArgb(255, 231, 98)) > 100);
            SaveCaptureWhenRequested(bitmap, "snake-ui-third-playing.png");
            return true;
        });
    }

    [Fact]
    public void GameOver_RendersResultWithoutBoardAndWithReplayButton()
    {
        _staTestRunner.Run(() =>
        {
            using GameForm form = new GameForm();
            form.Render(new GameViewModel(
                "game-over",
                new GameBoardViewModel(
                    false,
                    10,
                    10,
                    new List<GridCellViewModel>(),
                    new List<GridCellViewModel>(),
                    new List<DoorViewModel>(),
                    new List<BoardObjectViewModel>()),
                new GameStatusViewModel("第二關", 5, 20, 25, null),
                new List<RuleHintViewModel>(),
                false,
                true,
                false,
                0,
                new GameResultCardViewModel(
                    "Game Over",
                    "遊戲結束",
                    2,
                    5,
                    "碰到炸彈")));

            using Bitmap bitmap = Render(form);
            List<string> visibleText = GetVisibleText(form);
            GameBoardControl boardControl = FindControls<GameBoardControl>(form).Single();

            Assert.Contains("Game Over", visibleText);
            Assert.Contains("重玩", visibleText);
            Assert.DoesNotContain("開始", visibleText);
            Assert.False(boardControl.Visible);
            SaveCaptureWhenRequested(bitmap, "snake-ui-game-over.png");
            return true;
        });
    }

    private BoardObjectViewModel CreateObject(GameObjectType type, int x, int y)
    {
        return new BoardObjectViewModel(
            type,
            Guid.NewGuid().ToString(),
            new GridCellViewModel(x, y));
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

    private void SaveCaptureWhenRequested(Bitmap bitmap, string fileName)
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable("SNAKE_CAPTURE_UI"),
            "1",
            StringComparison.Ordinal))
        {
            return;
        }

        bitmap.Save(Path.Combine(Path.GetTempPath(), fileName));
    }
}
