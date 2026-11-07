using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Snake.Application;
using Snake.Application._GetGameScreenUseCase.Dto;
using Snake.Application._ReplenishTriggeredObjectUseCase.Port;
using Snake.Application._SetSnakeDirectionUseCase.Dto;
using Snake.Infrastructure;
using Snake.WinForms;

namespace Snake.Test
{
    public class GameFormAndRenderTests
    {
        private StaTestRunner _staTestRunner = new StaTestRunner();

        [Fact]
        public void GameForm_OwnsAFixed250MillisecondTimer()
        {
            _staTestRunner.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                using GameForm form = provider.GetRequiredService<GameForm>();
                FieldInfo timerField = typeof(GameForm).GetField(
                    "_gameTimer",
                    BindingFlags.Instance | BindingFlags.NonPublic)!;
                System.Windows.Forms.Timer timer = (System.Windows.Forms.Timer)timerField.GetValue(form)!;

                Assert.Equal(250, timer.Interval);
                return true;
            });
        }

        [Fact]
        public void FirstLevelReady_RendersBoardStatusRulesAndStartButton()
        {
            _staTestRunner.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                using GameForm form = provider.GetRequiredService<GameForm>();
                form.Show();
                System.Windows.Forms.Application.DoEvents();
                List<string> visibleText = GetVisibleText(form);

                Assert.Contains("第一關", visibleText);
                Assert.Contains("3 / 20", visibleText);
                Assert.Contains("過關進度 15%     目標 20 格", visibleText);
                Assert.Contains(visibleText, text => text.Contains("普通食物 +1 格", StringComparison.Ordinal));
                Assert.Contains("開始", visibleText);
                Assert.DoesNotContain("重玩", visibleText);
                Assert.Single(FindControls<Panel>(form).Where(panel => panel.AccessibleName == "遊戲棋盤"));
                return true;
            });
        }

        [Fact]
        public void FirstLevelReady_StartButtonIsEnabledAfterFormIsShown()
        {
            _staTestRunner.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                using GameForm form = provider.GetRequiredService<GameForm>();

                form.Show();
                System.Windows.Forms.Application.DoEvents();

                Button startButton = FindControls<Button>(form)
                    .Single(button => button.Visible && button.Text == "開始");
                Assert.True(startButton.Enabled);
                return true;
            });
        }

        [Fact]
        public void FirstLevelReady_CreatesOnePersistentCellForEachBoardPosition()
        {
            _staTestRunner.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                using GameForm form = provider.GetRequiredService<GameForm>();

                form.Show();
                System.Windows.Forms.Application.DoEvents();

                List<PictureBox> boardCells = FindControls<PictureBox>(form)
                    .Where(pictureBox => pictureBox.AccessibleName?.StartsWith("棋盤格 ", StringComparison.Ordinal) == true)
                    .ToList();
                Assert.Equal(100, boardCells.Count);
                Assert.All(boardCells, pictureBox => Assert.NotNull(pictureBox.Image));
                return true;
            });
        }

        [Fact]
        public void FirstLevelReady_StartButtonClickBeginsPlaying()
        {
            _staTestRunner.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                using GameForm form = provider.GetRequiredService<GameForm>();
                form.Show();
                System.Windows.Forms.Application.DoEvents();
                Button startButton = FindControls<Button>(form)
                    .Single(button => button.Visible && button.Text == "開始");

                startButton.PerformClick();
                System.Windows.Forms.Application.DoEvents();

                GameScreenDTO screen = provider.GetRequiredService<Snake.Application._GetGameScreenUseCase.GetGameScreenUseCase>()
                    .GetGameScreen();
                Assert.Equal(Snake.Application._GetGameScreenUseCase.Enums.GameScreenKind.FirstLevelPlaying, screen.ScreenKind);
                Assert.DoesNotContain(
                    FindControls<Button>(form),
                    button => button.Visible && button.Text == "開始");
                return true;
            });
        }

        [Fact]
        public void RepeatedScreenApplication_PreservesBoardAndRuleControlIdentity()
        {
            _staTestRunner.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                using GameForm form = provider.GetRequiredService<GameForm>();
                form.Show();
                System.Windows.Forms.Application.DoEvents();
                GameScreenDTO screen = provider.GetRequiredService<Snake.Application._GetGameScreenUseCase.GetGameScreenUseCase>()
                    .GetGameScreen();
                Panel boardBefore = FindControls<Panel>(form)
                    .Single(panel => panel.AccessibleName == "遊戲棋盤");
                List<Label> ruleLabelsBefore = FindControls<Label>(form)
                    .Where(label => label.Text.Contains("食物", StringComparison.Ordinal))
                    .ToList();
                List<PictureBox> boardCellsBefore = FindControls<PictureBox>(form)
                    .Where(pictureBox => pictureBox.AccessibleName?.StartsWith("棋盤格 ", StringComparison.Ordinal) == true)
                    .ToList();
                List<Image> boardImagesBefore = boardCellsBefore
                    .Select(pictureBox => pictureBox.Image)
                    .ToList();

                MethodInfo applyScreen = typeof(GameForm).GetMethod(
                    "ApplyScreen",
                    BindingFlags.Instance | BindingFlags.NonPublic)!;
                applyScreen.Invoke(form, new object[] { screen });

                Assert.Same(
                    boardBefore,
                    FindControls<Panel>(form).Single(panel => panel.AccessibleName == "遊戲棋盤"));
                List<Label> ruleLabelsAfter = FindControls<Label>(form)
                    .Where(label => label.Text.Contains("食物", StringComparison.Ordinal))
                    .ToList();
                Assert.Equal(ruleLabelsBefore.Count, ruleLabelsAfter.Count);
                for (int index = 0; index < ruleLabelsBefore.Count; index++)
                {
                    Assert.Same(ruleLabelsBefore[index], ruleLabelsAfter[index]);
                }

                List<PictureBox> boardCellsAfter = FindControls<PictureBox>(form)
                    .Where(pictureBox => pictureBox.AccessibleName?.StartsWith("棋盤格 ", StringComparison.Ordinal) == true)
                    .ToList();
                Assert.Equal(boardCellsBefore.Count, boardCellsAfter.Count);
                for (int index = 0; index < boardCellsBefore.Count; index++)
                {
                    Assert.Same(boardCellsBefore[index], boardCellsAfter[index]);
                    Assert.Same(boardImagesBefore[index], boardCellsAfter[index].Image);
                }

                return true;
            });
        }

        [Theory]
        [InlineData(Keys.Up, 0, -1)]
        [InlineData(Keys.Down, 0, 1)]
        [InlineData(Keys.Left, -1, 0)]
        [InlineData(Keys.Right, 1, 0)]
        public void DirectionKeyMessage_WithFocusedButton_DispatchesExactlyOneDirectionRequest(
            Keys key,
            int expectedX,
            int expectedY)
        {
            _staTestRunner.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                using GameForm form = provider.GetRequiredService<GameForm>();
                List<SnakeDirectionDTO> directions = new List<SnakeDirectionDTO>();
                form.DirectionRequested += (_, eventArgs) => directions.Add(eventArgs.Direction);
                form.Show();
                System.Windows.Forms.Application.DoEvents();
                Button startButton = FindControls<Button>(form).Single(button => button.Visible && button.Text == "開始");
                form.ActiveControl = startButton;
                Assert.Same(startButton, form.ActiveControl);
                Message message = Message.Create(startButton.Handle, 0x0100, new IntPtr((int)key), IntPtr.Zero);

                bool handled = startButton.PreProcessMessage(ref message);

                Assert.True(handled);
                SnakeDirectionDTO direction = Assert.Single(directions);
                Assert.Equal(expectedX, direction.X);
                Assert.Equal(expectedY, direction.Y);
                return true;
            });
        }

        private ServiceProvider CreateProvider()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddApplication();
            services.AddInfrastructure();
            services.AddSingleton<ISpawnPositionSelector, FirstLegalSpawnPositionSelector>();
            services.AddTransient<GameForm>();
            return services.BuildServiceProvider();
        }

        private List<string> GetVisibleText(Control root)
        {
            return FindControls<Control>(root)
                .Where(control => control.Visible && !string.IsNullOrWhiteSpace(control.Text))
                .Select(control => control.Text)
                .ToList();
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
    }
}
