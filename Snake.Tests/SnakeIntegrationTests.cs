using Snake;

namespace Snake.Tests;

public class SnakeIntegrationTests
{
    private GameTestDriver _driver = new GameTestDriver();

    [Fact]
    public void PlayerFlow_CompletesThreeLevelsAndRestartsWithFreshFirstLevel()
    {
        SnakeGame game = new SnakeGame();
        game.SetTestRandom(new FixedRandom());
        game.ShowFirstLevelReadyScreen();
        Assert.Equal(GamePhase.Ready, game.Phase);

        game.StartFirstLevel();
        Assert.Contains(GameObjectType.Strawberry, game.Objects.Values);
        game = CreateNextObjectGame(game, 1, 19, GameObjectType.Strawberry);
        _driver.Advance(game);
        Assert.Equal(GamePhase.Ready, game.Phase);
        Assert.Equal(2, game.Level);
        Assert.Equal(20, game.LastCompletedLength);
        game.ShowSecondLevelReadyScreen();
        game.StartSecondLevel();
        Assert.Contains(GameObjectType.Blueberry, game.Objects.Values);
        game = CreateNextObjectGame(game, 2, 18, GameObjectType.Blueberry);
        _driver.Advance(game);
        Assert.Equal(GamePhase.Ready, game.Phase);
        Assert.Equal(3, game.Level);
        Assert.Equal(20, game.LastCompletedLength);
        game.ShowThirdLevelReadyScreen();
        game.StartThirdLevel();
        Assert.Contains(GameObjectType.Bomb, game.Objects.Values);

        game = CreateNextObjectGame(game, 3, 49, GameObjectType.Strawberry);
        _driver.Advance(game);
        Assert.Equal(GamePhase.SuccessPending, game.Phase);
        game.ShowThirdLevelSuccessScreen();
        Assert.Equal(GamePhase.Success, game.Phase);
        Assert.Equal(100, game.ProgressPercent);

        game.RestartGame();
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(1, game.Level);
        Assert.Equal(3, game.SnakeLength);
        Assert.Equal(0, game.LastCompletedLevel);
        Assert.Equal(0, game.ShieldCount);
        Assert.Contains(GameObjectType.Strawberry, game.Objects.Values);
        Assert.DoesNotContain(GameObjectType.Bomb, game.Objects.Values);
    }

    [Fact]
    public void PlayerFlow_ShieldRescueContinuesSameLevelAndTerminalStateStopsChanges()
    {
        SnakeGame rescued = _driver.CreateGame(2, GamePhase.Playing, new System.Drawing.Point[] { new(2, 2), new(2, 3), new(1, 3) }.ToList(), Direction.Down, shieldCount: 1);
        _driver.Advance(rescued);
        Assert.Equal(GamePhase.Playing, rescued.Phase);
        Assert.Equal(2, rescued.Level);
        Assert.Equal(0, rescued.ShieldCount);
        Assert.Equal(rescued.SafePath[2], rescued.Snake[0]);

        SnakeGame failed = _driver.CreateGame(1, GamePhase.DeathPending);
        failed.SetTestScenario(1, GamePhase.DeathPending);
        failed.ShowGameOverScreen();
        Point[] deadSnake = failed.Snake.ToArray();
        var deadObjects = failed.Objects.ToDictionary(entry => entry.Key, entry => entry.Value);
        failed.SetDirection(Direction.Up);
        failed.MaintainLevelObjects();
        failed.SetTestElapsedMilliseconds(100000);
        failed.UpdateGameTime();
        Assert.Equal(GamePhase.GameOver, failed.Phase);
        Assert.Equal(deadSnake, failed.Snake);
        Assert.Equal(deadObjects, failed.Objects);
    }

    private SnakeGame CreateNextObjectGame(SnakeGame game, int level, int length, GameObjectType objectType)
    {
        List<System.Drawing.Point> snake = _driver.SnakeOnSafePath(game, length);
        Direction direction = _driver.DirectionAlongSafePath(game, length);
        System.Drawing.Point target = game.SafePath[length];
        SnakeGame configured = new SnakeGame();
        configured.SetTestScenario(level, GamePhase.Playing, length, snake, new Dictionary<System.Drawing.Point, GameObjectType> { [target] = objectType }, direction: direction);
        configured.SetTestRandom(new FixedRandom());
        return configured;
    }
}
