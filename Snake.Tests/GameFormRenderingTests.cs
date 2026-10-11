using System.Drawing;
using Snake;

namespace Snake.Tests;

public class GameFormRenderingTests
{
    private StaTestRunner _sta = new StaTestRunner();

    [Fact]
    public void WinFormsScreens_RenderInStaWithExpectedTextRegionsAndPixels()
    {
        _sta.Run(() =>
        {
            SnakeGame firstReady = new SnakeGame();
            firstReady.ShowFirstLevelReadyScreen();
            firstReady.UpdateLevelStatus(1);
            AssertRendered(firstReady, new[] { "第1關", "3 / 20", "目標 20 格", "過關進度　15%", "開始", "普通食物：草莓 +1 格", "棋盤邊界：可以穿越" }, new[] { "Game Over", "闖關成功" }, new Rectangle(50, 90, 800, 800), Color.FromArgb(255, 253, 244));

            SnakeGame secondReady = new SnakeGame();
            secondReady.SetTestScenario(2, GamePhase.Ready, lastCompletedLevel: 1, lastCompletedLength: 20);
            secondReady.ShowSecondLevelReadyScreen();
            AssertRendered(secondReady, new[] { "第1關通關　通關蛇長：20 格", "第2關", "3 / 20", "目標 20 格", "過關進度　15%", "保命次數　0", "開始", "特殊食物：藍莓 +2 格" }, new[] { "Game Over", "闖關成功" }, new Rectangle(850, 80, 650, 850), Color.FromArgb(255, 253, 246));

            SnakeGame thirdReady = new SnakeGame();
            thirdReady.SetTestScenario(3, GamePhase.Ready, lastCompletedLevel: 2, lastCompletedLength: 51);
            thirdReady.ShowThirdLevelReadyScreen();
            AssertRendered(thirdReady, new[] { "第2關通關　通關蛇長：51 格", "第3關", "門狀態：門一開啟、門二關閉", "迷宮牆：不可穿越", "炸彈：立即死亡", "開始" }, new[] { "Game Over", "闖關成功" }, new Rectangle(850, 80, 650, 850), Color.FromArgb(255, 253, 246));

            GameTestDriver driver = new GameTestDriver();
            SnakeGame playing = driver.CreateGame(2, GamePhase.Ready);
            playing.SetTestRandom(new FixedRandom());
            playing.StartSecondLevel();
            playing.UpdateLevelStatus(2);
            AssertRendered(playing, new[] { "第2關", "3 / 20", "目標 20 格", "過關進度　15%", "保命次數　0", "特殊食物：藍莓 +2 格" }, new[] { "開始", "Game Over", "闖關成功" }, new Rectangle(850, 80, 650, 850), Color.FromArgb(255, 253, 246));

            SnakeGame gameOver = new SnakeGame();
            gameOver.SetTestScenario(2, GamePhase.DeathPending, 3);
            gameOver.ShowGameOverScreen();
            AssertRendered(gameOver, new[] { "Game Over", "第 2 關失敗", "最後蛇長：3 / 20", "過關進度　15%", "重玩" }, new[] { "開始", "闖關成功" }, new Rectangle(1000, 730, 400, 180), Color.FromArgb(71, 123, 84));

            SnakeGame success = new SnakeGame();
            success.SetTestScenario(3, GamePhase.SuccessPending, 50);
            success.ShowThirdLevelSuccessScreen();
            AssertRendered(success, new[] { "闖關成功", "蛇長：50 / 50", "過關進度　100%", "重玩" }, new[] { "Game Over", "開始" }, new Rectangle(1000, 730, 400, 180), Color.FromArgb(71, 123, 84));

            SnakeGame successOverTarget = new SnakeGame();
            successOverTarget.SetTestScenario(3, GamePhase.SuccessPending, 51);
            successOverTarget.ShowThirdLevelSuccessScreen();
            AssertRendered(successOverTarget, new[] { "闖關成功", "蛇長：51 / 50", "過關進度　100%", "重玩" }, new[] { "Game Over", "開始" }, new Rectangle(1000, 730, 400, 180), Color.FromArgb(71, 123, 84));

            SnakeGame fireOver = new SnakeGame();
            fireOver.SetTestScenario(2, GamePhase.DeathPending, 0, new List<Point>(), failedByFire: true, deathCause: DeathCause.FatalFire);
            fireOver.ShowGameOverScreen();
            AssertRendered(fireOver, new[] { "Game Over", "最後蛇長：0 / 20", "過關進度　0%", "火焰致死進度為 0%", "重玩" }, new[] { "開始", "闖關成功" }, new Rectangle(950, 490, 600, 100), Color.FromArgb(225, 231, 223));

            GameTestDriver clickDriver = new GameTestDriver();
            SnakeGame clickStart = clickDriver.CreateGame(1, GamePhase.Ready);
            using GameForm startForm = new GameForm(clickStart);
            startForm.StopFrameTimerForTest();
            startForm.Show();
            startForm.ClickAtForTest(new Point(1100, 820));
            Assert.Equal(GamePhase.Playing, startForm.Game.Phase);
            using (Bitmap startBitmap = new Bitmap(startForm.ClientSize.Width, startForm.ClientSize.Height))
            {
                startForm.DrawToBitmap(startBitmap, new Rectangle(Point.Empty, startBitmap.Size));
                Assert.DoesNotContain("開始", startForm.RenderedText);
            }

            SnakeGame clickReplay = new SnakeGame();
            clickReplay.SetTestScenario(1, GamePhase.DeathPending);
            clickReplay.ShowGameOverScreen();
            using GameForm replayForm = new GameForm(clickReplay);
            replayForm.StopFrameTimerForTest();
            replayForm.Show();
            replayForm.ClickAtForTest(new Point(1100, 820));
            Assert.Equal(GamePhase.Playing, replayForm.Game.Phase);
            Assert.Equal(1, replayForm.Game.Level);
            using (Bitmap replayBitmap = new Bitmap(replayForm.ClientSize.Width, replayForm.ClientSize.Height))
            {
                replayForm.DrawToBitmap(replayBitmap, new Rectangle(Point.Empty, replayBitmap.Size));
                Assert.DoesNotContain("Game Over", replayForm.RenderedText);
                Assert.Contains("第1關", replayForm.RenderedText);
                Assert.DoesNotContain("開始", replayForm.RenderedText);
            }
            return true;
        });
    }

    private void AssertRendered(SnakeGame game, string[] expectedText, string[] absentText, Rectangle pixelRegion, Color expectedPixel)
    {
        using GameForm form = new GameForm(game);
        form.StopFrameTimerForTest();
        form.Show();
        using Bitmap bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));

        foreach (string text in expectedText)
        {
            Assert.Contains(text, form.RenderedText);
        }

        foreach (string text in absentText)
        {
            Assert.DoesNotContain(text, form.RenderedText);
        }

        int matchingPixels = 0;
        for (int y = Math.Max(0, pixelRegion.Top); y < Math.Min(bitmap.Height, pixelRegion.Bottom); y++)
        {
            for (int x = Math.Max(0, pixelRegion.Left); x < Math.Min(bitmap.Width, pixelRegion.Right); x++)
            {
                if (bitmap.GetPixel(x, y).ToArgb() == expectedPixel.ToArgb())
                {
                    matchingPixels++;
                }
            }
        }

        Assert.True(matchingPixels > 100, $"Expected rendered color {expectedPixel} in {pixelRegion}; found {matchingPixels} matching pixels.");
    }
}
