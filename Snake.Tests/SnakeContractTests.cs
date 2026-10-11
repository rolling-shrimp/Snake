using System.Drawing;
using Snake;

namespace Snake.Tests;

public class SnakeContractTests
{
    private GameTestDriver _driver = new GameTestDriver();

    [Fact]
    public void T01_ShowFirstLevelReadyScreen_InitializesReadyStateAndRejectsWrongPhase()
    {
        SnakeGame game = new SnakeGame();
        game.ShowFirstLevelReadyScreen();

        Assert.Equal(GamePhase.Ready, game.Phase);
        Assert.Equal(1, game.Level);
        Assert.Equal(3, game.SnakeLength);
        Assert.Equal(Direction.Right, game.CurrentDirection);
        Assert.Equal(15, game.ProgressPercent);
        Assert.Empty(game.Objects);
        Assert.Equal(3, game.Snake.Count);

        SnakeGame playing = _driver.CreateGame(1, GamePhase.Playing);
        var previousSnake = playing.Snake.ToArray();
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(playing.ShowFirstLevelReadyScreen);
        Assert.True(exception.Data.Contains("Snake.ContractViolation"));
        Assert.Equal(previousSnake, playing.Snake);
        Assert.Equal(GamePhase.Playing, playing.Phase);
    }

    [Fact]
    public void T02_StartFirstLevel_SpawnsStrawberryAndDoesNotRestartWhenRepeated()
    {
        SnakeGame game = new SnakeGame();
        game.ShowFirstLevelReadyScreen();
        game.SetTestRandom(new FixedRandom());
        game.StartFirstLevel();

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(3, game.SnakeLength);
        Assert.Single(game.Objects);
        Assert.Contains(GameObjectType.Strawberry, game.Objects.Values);
        _driver.AssertObjectsAreLegal(game);
        var objectsBefore = game.Objects.ToDictionary(entry => entry.Key, entry => entry.Value);
        long nextMoveBefore = game.NextMoveAtMilliseconds;
        game.StartFirstLevel();
        Assert.Equal(objectsBefore, game.Objects);
        Assert.Equal(nextMoveBefore, game.NextMoveAtMilliseconds);
    }

    [Fact]
    public void T03_SetDirection_QueuesValidInputIgnoresReverseAndRejectsInvalidValue()
    {
        List<Point> snake = new List<Point> { new Point(5, 5), new Point(4, 5), new Point(3, 5) };
        SnakeGame game = _driver.CreateGame(1, GamePhase.Playing, snake, Direction.Right);
        Point originalHead = game.Snake[0];
        game.SetDirection(Direction.Up);
        Assert.Equal(originalHead, game.Snake[0]);
        Assert.Equal(3, game.SnakeLength);
        _driver.Advance(game);
        Assert.Equal(Direction.Up, game.CurrentDirection);
        Assert.Equal(new Point(5, 4), game.Snake[0]);

        foreach ((Direction requested, Point expectedHead) in new[]
        {
            (Direction.Right, new Point(6, 5)),
            (Direction.Down, new Point(5, 6))
        })
        {
            SnakeGame acceptedGame = _driver.CreateGame(1, GamePhase.Playing, snake, Direction.Right);
            acceptedGame.SetDirection(requested);
            _driver.Advance(acceptedGame);
            Assert.Equal(requested, acceptedGame.CurrentDirection);
            Assert.Equal(expectedHead, acceptedGame.Snake[0]);
        }

        SnakeGame reverseGame = _driver.CreateGame(1, GamePhase.Playing, snake, Direction.Right);
        reverseGame.SetDirection(Direction.Left);
        _driver.Advance(reverseGame);
        Assert.Equal(Direction.Right, reverseGame.CurrentDirection);
        Assert.Equal(new Point(6, 5), reverseGame.Snake[0]);

        var previousSnake = reverseGame.Snake.ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => reverseGame.SetDirection((Direction)99));
        Assert.Equal(previousSnake, reverseGame.Snake);

        SnakeGame readyGame = new SnakeGame();
        readyGame.ShowFirstLevelReadyScreen();
        readyGame.SetDirection(Direction.Up);
        Assert.Equal(Direction.Right, readyGame.CurrentDirection);
    }

    [Fact]
    public void T04_AdvanceFirstLevelStep_Uses250MillisecondsWrapsEatsAndCompletesAt20()
    {
        SnakeGame timedGame = _driver.CreateGame(1, GamePhase.Playing, new List<Point> { new Point(4, 4), new Point(3, 4), new Point(2, 4) }, Direction.Right);
        Point initialHead = timedGame.Snake[0];
        timedGame.SetTestElapsedMilliseconds(249);
        timedGame.AdvanceFirstLevelStep();
        Assert.Equal(initialHead, timedGame.Snake[0]);
        timedGame.SetTestElapsedMilliseconds(250);
        timedGame.AdvanceFirstLevelStep();
        Assert.Equal(new Point(5, 4), timedGame.Snake[0]);

        List<(List<Point> Snake, Direction Direction, Point Expected)> boundaryCases = new List<(List<Point>, Direction, Point)>
        {
            (new List<Point> { new Point(9, 4), new Point(8, 4), new Point(7, 4) }, Direction.Right, new Point(0, 4)),
            (new List<Point> { new Point(0, 4), new Point(1, 4), new Point(2, 4) }, Direction.Left, new Point(9, 4)),
            (new List<Point> { new Point(4, 0), new Point(4, 1), new Point(4, 2) }, Direction.Up, new Point(4, 9)),
            (new List<Point> { new Point(4, 9), new Point(4, 8), new Point(4, 7) }, Direction.Down, new Point(4, 0))
        };
        foreach ((List<Point> snake, Direction direction, Point expected) in boundaryCases)
        {
            SnakeGame game = _driver.CreateGame(1, GamePhase.Playing, snake, direction);
            _driver.Advance(game);
            Assert.Equal(expected, game.Snake[0]);
        }

        SnakeGame eatingGame = CreateNextObjectGame(1, 3, GameObjectType.Strawberry);
        _driver.Advance(eatingGame);
        Assert.Equal(4, eatingGame.SnakeLength);

        List<Point> loop = new List<Point> { new Point(2, 2), new Point(2, 3), new Point(1, 3) };
        SnakeGame collisionGame = _driver.CreateGame(1, GamePhase.Playing, loop, Direction.Down);
        _driver.Advance(collisionGame);
        Assert.Equal(GamePhase.DeathPending, collisionGame.Phase);
        collisionGame.ShowGameOverScreen();
        Assert.Equal(GamePhase.GameOver, collisionGame.Phase);

        SnakeGame belowTarget = _driver.CreateGame(1, GamePhase.Playing, snakeLength: 19);
        _driver.Advance(belowTarget);
        Assert.Equal(GamePhase.Playing, belowTarget.Phase);
        Assert.Equal(19, belowTarget.SnakeLength);

        foreach ((int length, int expectedCompletedLength) in new[] { (19, 20), (20, 21) })
        {
            SnakeGame completionGame = CreateNextObjectGame(1, length, GameObjectType.Strawberry);
            _driver.Advance(completionGame);
            Assert.Equal(2, completionGame.Level);
            Assert.Equal(expectedCompletedLength, completionGame.LastCompletedLength);
        }

        SnakeGame completion = CreateNextObjectGame(1, 18, GameObjectType.Blueberry);
        _driver.Advance(completion);
        Assert.Equal(2, completion.Level);
        Assert.Equal(20, completion.LastCompletedLength);
        Assert.Equal(GamePhase.Ready, completion.Phase);
        Assert.Equal(3, completion.SnakeLength);
        Assert.Equal(15, completion.ProgressPercent);
        Assert.Equal(0, completion.ShieldCount);
        Assert.Empty(completion.Objects);
    }

    [Fact]
    public void T05_ShowSecondLevelReadyScreen_PreservesPreviousResultAndRejectsInvalidStage()
    {
        SnakeGame game = _driver.CreateGame(2, GamePhase.Ready);
        game.ShowSecondLevelReadyScreen();
        Assert.True(game.HasPreviousLevelResult);
        Assert.Equal(1, game.LastCompletedLevel);
        Assert.Equal(20, game.LastCompletedLength);
        Assert.Equal(3, game.SnakeLength);
        Assert.Equal(0, game.ShieldCount);
        Assert.Empty(game.Objects);

        SnakeGame invalid = _driver.CreateGame(2, GamePhase.Playing);
        var previousSnake = invalid.Snake.ToArray();
        Assert.Throws<InvalidOperationException>(invalid.ShowSecondLevelReadyScreen);
        Assert.Equal(previousSnake, invalid.Snake);
        Assert.Equal(GamePhase.Playing, invalid.Phase);
    }

    [Fact]
    public void T06_StartSecondLevel_SpawnsOnlyAllowedStartingObjects()
    {
        SnakeGame game = _driver.CreateGame(2, GamePhase.Ready);
        game.SetTestRandom(new FixedRandom());
        game.StartSecondLevel();

        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(3, game.SnakeLength);
        Assert.Equal(0, game.ShieldCount);
        Assert.Equal(new HashSet<GameObjectType> { GameObjectType.Strawberry, GameObjectType.Blueberry, GameObjectType.Shield, GameObjectType.Fire }, game.Objects.Values.ToHashSet());
        Assert.DoesNotContain(GameObjectType.Bomb, game.Objects.Values);
        Assert.Empty(game.Walls);
        var objectsBefore = game.Objects.ToDictionary(entry => entry.Key, entry => entry.Value);
        game.StartSecondLevel();
        Assert.Equal(objectsBefore, game.Objects);
        _driver.AssertObjectsAreLegal(game);
    }

    [Fact]
    public void T07_AdvanceSecondLevel_AppliesItemsDeathShieldAnd20LengthCompletion()
    {
        foreach ((GameObjectType item, int initialLength, int expectedLength) in new[]
        {
            (GameObjectType.Strawberry, 3, 4),
            (GameObjectType.Blueberry, 3, 5),
            (GameObjectType.Fire, 3, 1)
        })
        {
            SnakeGame itemGame = CreateNextObjectGame(2, initialLength, item);
            _driver.Advance(itemGame);
            Assert.Equal(expectedLength, itemGame.SnakeLength);
        }

        SnakeGame shieldGame = CreateNextObjectGame(2, 3, GameObjectType.Shield, 0, 1, 0);
        _driver.Advance(shieldGame);
        Assert.Equal(1, shieldGame.ShieldCount);
        Assert.Contains(GameObjectType.Shield, shieldGame.Objects.Values);

        SnakeGame nonFatalFire = CreateNextObjectGame(2, 3, GameObjectType.Fire, 1, 1, 1);
        _driver.Advance(nonFatalFire);
        Assert.Equal(1, nonFatalFire.ShieldCount);
        Assert.Equal(GamePhase.Playing, nonFatalFire.Phase);

        SnakeGame fatalFire = CreateNextObjectGame(2, 2, GameObjectType.Fire);
        _driver.Advance(fatalFire);
        Assert.Equal(GamePhase.DeathPending, fatalFire.Phase);
        Assert.Equal(DeathCause.FatalFire, fatalFire.LastDeathCause);

        SnakeGame rescuedBody = _driver.CreateGame(2, GamePhase.Playing, new List<Point> { new Point(2, 2), new Point(2, 3), new Point(1, 3) }, Direction.Down, shieldCount: 1);
        _driver.Advance(rescuedBody);
        Assert.Equal(GamePhase.Playing, rescuedBody.Phase);
        Assert.Equal(0, rescuedBody.ShieldCount);

        SnakeGame unprotectedBody = _driver.CreateGame(2, GamePhase.Playing, new List<Point> { new Point(2, 2), new Point(2, 3), new Point(1, 3) }, Direction.Down);
        _driver.Advance(unprotectedBody);
        Assert.Equal(GamePhase.DeathPending, unprotectedBody.Phase);

        SnakeGame belowTarget = _driver.CreateGame(2, GamePhase.Playing, snakeLength: 19);
        _driver.Advance(belowTarget);
        Assert.Equal(GamePhase.Playing, belowTarget.Phase);
        Assert.Equal(19, belowTarget.SnakeLength);

        foreach ((int initialLength, int expectedCompletedLength) in new[] { (18, 20), (19, 21) })
        {
            SnakeGame completion = CreateNextObjectGame(2, initialLength, GameObjectType.Blueberry);
            _driver.Advance(completion);
            Assert.Equal(3, completion.Level);
            Assert.Equal(expectedCompletedLength, completion.LastCompletedLength);
            Assert.Equal(GamePhase.Ready, completion.Phase);
            Assert.Equal(3, completion.SnakeLength);
            Assert.Equal(6, completion.ProgressPercent);
            Assert.Equal(0, completion.ShieldCount);
            Assert.Empty(completion.Objects);
        }
    }

    [Fact]
    public void T08_ShowThirdLevelReadyScreen_PreservesSecondResultAndMazeCapacity()
    {
        SnakeGame game = _driver.CreateGame(3, GamePhase.Ready);
        game.ShowThirdLevelReadyScreen();
        Assert.Equal(GamePhase.Ready, game.Phase);
        Assert.Equal(3, game.SnakeLength);
        Assert.Equal(6, game.ProgressPercent);
        Assert.Equal(0, game.ShieldCount);
        Assert.True(game.HasPreviousLevelResult);
        Assert.NotEmpty(game.Walls);
        Assert.Equal(2, game.Doors.Count);
        Assert.Empty(game.Objects);
        Assert.True(game.IsDoorOpen(0));
        Assert.False(game.IsDoorOpen(1));

        SnakeGame invalid = _driver.CreateGame(3, GamePhase.Playing);
        Assert.Throws<InvalidOperationException>(invalid.ShowThirdLevelReadyScreen);
    }

    [Fact]
    public void T09_StartThirdLevel_SpawnsFiveKindsAndDoesNotRestartWhenRepeated()
    {
        SnakeGame game = _driver.CreateGame(3, GamePhase.Ready);
        game.SetTestRandom(new FixedRandom());
        game.StartThirdLevel();
        Assert.Equal(GamePhase.Playing, game.Phase);
        Assert.Equal(new HashSet<GameObjectType> { GameObjectType.Strawberry, GameObjectType.Blueberry, GameObjectType.Shield, GameObjectType.Fire, GameObjectType.Bomb }, game.Objects.Values.ToHashSet());
        Assert.Equal(3, game.SnakeLength);
        Assert.Equal(0, game.ShieldCount);
        var before = game.Objects.ToDictionary(entry => entry.Key, entry => entry.Value);
        game.StartThirdLevel();
        Assert.Equal(before, game.Objects);
        Assert.True(game.IsDoorOpen(0));
        Assert.False(game.IsDoorOpen(1));
        _driver.AssertObjectsAreLegal(game);
    }

    [Fact]
    public void T10_AdvanceThirdLevel_HandlesItemsMazeCollisionsOpenDoorAndCompletion()
    {
        foreach ((GameObjectType item, int initialLength, int expectedLength) in new[]
        {
            (GameObjectType.Strawberry, 3, 4),
            (GameObjectType.Blueberry, 3, 5),
            (GameObjectType.Fire, 3, 1)
        })
        {
            SnakeGame itemGame = CreateNextObjectGame(3, initialLength, item);
            _driver.Advance(itemGame);
            Assert.Equal(expectedLength, itemGame.SnakeLength);
        }

        SnakeGame bombGame = CreateNextObjectGame(3, 3, GameObjectType.Bomb);
        _driver.Advance(bombGame);
        Assert.Equal(GamePhase.DeathPending, bombGame.Phase);
        Assert.Equal(DeathCause.Bomb, bombGame.LastDeathCause);

        SnakeGame outerWall = _driver.CreateGame(3, GamePhase.Playing, new List<Point> { new Point(1, 1), new Point(2, 1), new Point(3, 1) }, Direction.Up);
        _driver.Advance(outerWall);
        Assert.Equal(DeathCause.WallCollision, outerWall.LastDeathCause);

        SnakeGame innerWall = _driver.CreateGame(3, GamePhase.Playing, new List<Point> { new Point(6, 2), new Point(5, 2), new Point(4, 2) }, Direction.Up);
        _driver.Advance(innerWall);
        Assert.Equal(DeathCause.WallCollision, innerWall.LastDeathCause);

        SnakeGame closedDoor = _driver.CreateGame(3, GamePhase.Playing, new List<Point> { new Point(7, 2), new Point(8, 2), new Point(8, 3) }, Direction.Up, firstDoorOpen: false);
        _driver.Advance(closedDoor);
        Assert.Equal(DeathCause.ClosedDoor, closedDoor.LastDeathCause);

        SnakeGame openDoor = _driver.CreateGame(3, GamePhase.Playing, new List<Point> { new Point(7, 2), new Point(8, 2), new Point(8, 3) }, Direction.Up, firstDoorOpen: true);
        _driver.Advance(openDoor);
        Assert.Equal(GamePhase.Playing, openDoor.Phase);
        Assert.Equal(new Point(7, 1), openDoor.Snake[0]);

        SnakeGame body = _driver.CreateGame(3, GamePhase.Playing, new List<Point> { new Point(2, 2), new Point(2, 3), new Point(1, 3) }, Direction.Down);
        _driver.Advance(body);
        Assert.Equal(DeathCause.BodyCollision, body.LastDeathCause);

        SnakeGame shieldedWall = _driver.CreateGame(3, GamePhase.Playing, new List<Point> { new Point(1, 1), new Point(2, 1), new Point(3, 1) }, Direction.Up, shieldCount: 1);
        _driver.Advance(shieldedWall);
        Assert.Equal(GamePhase.Playing, shieldedWall.Phase);
        Assert.Equal(0, shieldedWall.ShieldCount);
        Assert.Equal(3, shieldedWall.SnakeLength);

        SnakeGame completion = CreateNextObjectGame(3, 49, GameObjectType.Strawberry);
        _driver.Advance(completion);
        Assert.Equal(GamePhase.SuccessPending, completion.Phase);
        Assert.Equal(50, completion.SnakeLength);
        Assert.Equal(100, completion.ProgressPercent);
    }

    [Fact]
    public void T11_UpdateThirdLevelDoor_WarnsSwitchesAndDefersClosingUntilSnakeLeaves()
    {
        SnakeGame game = _driver.CreateGame(3, GamePhase.Playing, nextMoveAtMilliseconds: long.MaxValue);
        game.SetTestElapsedMilliseconds(3999);
        game.UpdateGameTime();
        Assert.False(game.IsDoorWarning(0));
        game.SetTestElapsedMilliseconds(4000);
        game.UpdateGameTime();
        Assert.True(game.IsDoorWarning(0));
        Assert.True(game.IsDoorOpen(0));

        game.SetTestElapsedMilliseconds(5999);
        game.UpdateGameTime();
        Assert.True(game.IsDoorOpen(0));
        game.SetTestElapsedMilliseconds(6000);
        game.UpdateGameTime();
        Assert.False(game.IsDoorOpen(0));
        Assert.True(game.IsDoorOpen(1));

        List<Point> snakeOnDoor = new List<Point> { new Point(7, 1), new Point(7, 2), new Point(7, 3) };
        SnakeGame occupied = _driver.CreateGame(3, GamePhase.Playing, snakeOnDoor, Direction.Right, nextMoveAtMilliseconds: 6000);
        occupied.SetTestElapsedMilliseconds(6000);
        occupied.UpdateThirdLevelDoor();
        Assert.True(occupied.IsDoorOpen(0));
        Assert.True(occupied.IsDoorWarning(0));
        Assert.True(occupied.IsDoorOpen(1));

        _driver.Advance(occupied, 6000);
        occupied.SetDirection(Direction.Down);
        _driver.Advance(occupied, 6250);
        occupied.SetDirection(Direction.Left);
        _driver.Advance(occupied, 6500);
        Assert.False(occupied.IsDoorOpen(0));
        Assert.False(occupied.IsDoorWarning(0));
    }

    [Fact]
    public void T12_RescueWithShield_ConsumesOneChargeRestoresLengthAndRejectsInvalidUse()
    {
        foreach (DeathCause cause in Enum.GetValues<DeathCause>())
        {
            SnakeGame game = _driver.CreateGame(3, GamePhase.DeathPending, snakeLength: 5, shieldCount: 1);
            game.SetTestScenario(3, GamePhase.DeathPending, snakeLength: 5, shieldCount: 1, deathCause: cause);
            game.RescueWithShield(cause);
            Assert.Equal(GamePhase.Playing, game.Phase);
            Assert.Equal(5, game.SnakeLength);
            Assert.Equal(5, game.Snake.Count);
            Assert.Equal(0, game.ShieldCount);
            Assert.Equal(game.SafePath[4], game.Snake[0]);
            _driver.AssertObjectsAreLegal(game);
        }

        SnakeGame invalidCause = _driver.CreateGame(2, GamePhase.DeathPending, shieldCount: 1);
        invalidCause.SetTestScenario(2, GamePhase.DeathPending, shieldCount: 1);
        var previousObjects = invalidCause.Objects.ToDictionary(entry => entry.Key, entry => entry.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidCause.RescueWithShield((DeathCause)99));
        Assert.Equal(1, invalidCause.ShieldCount);
        Assert.Equal(previousObjects, invalidCause.Objects);

        SnakeGame noShield = _driver.CreateGame(2, GamePhase.DeathPending);
        noShield.SetTestScenario(2, GamePhase.DeathPending);
        Assert.Throws<InvalidOperationException>(() => noShield.RescueWithShield(DeathCause.BodyCollision));
        Assert.Equal(0, noShield.ShieldCount);
        Assert.Equal(GamePhase.DeathPending, noShield.Phase);
    }

    [Fact]
    public void T13_UpdateLevelStatus_ClampsProgressPreservesGameStateAndValidatesInputs()
    {
        foreach (int length in new[] { 3, 19, 20, 21, 49, 50, 51 })
        {
            foreach (int level in new[] { 1, 2, 3 })
            {
                SnakeGame game = _driver.CreateGame(level, GamePhase.Ready, snakeLength: length);
                game.UpdateLevelStatus(level);
                Assert.Equal(level, game.StatusLevel);
                Assert.Equal(length, game.SnakeLength);
                int target = level < 3 ? 20 : 50;
                Assert.Equal(Math.Clamp((int)Math.Floor(length * 100.0 / target), 0, 100), game.ProgressPercent);
            }
        }

        SnakeGame fireDeath = new SnakeGame();
        fireDeath.SetTestScenario(2, GamePhase.GameOver, 0, new List<Point>(), failedByFire: true);
        int oldLength = fireDeath.SnakeLength;
        GamePhase oldPhase = fireDeath.Phase;
        Assert.Equal(0, fireDeath.ProgressPercent);
        fireDeath.UpdateLevelStatus(2);
        Assert.Equal(oldLength, fireDeath.SnakeLength);
        Assert.Equal(oldPhase, fireDeath.Phase);
        Assert.True(fireDeath.FailedByFire);

        Assert.Throws<ArgumentOutOfRangeException>(() => fireDeath.UpdateLevelStatus(0));
        SnakeGame uninitialized = new SnakeGame();
        Assert.Throws<InvalidOperationException>(() => uninitialized.UpdateLevelStatus(1));
    }

    [Fact]
    public void T14_MaintainLevelObjects_EnforcesAllowedTypesLimitsAndWaitsWhenFull()
    {
        Dictionary<int, HashSet<GameObjectType>> allowedByLevel = new Dictionary<int, HashSet<GameObjectType>>
        {
            [1] = new HashSet<GameObjectType> { GameObjectType.Strawberry },
            [2] = new HashSet<GameObjectType> { GameObjectType.Strawberry, GameObjectType.Blueberry, GameObjectType.Shield, GameObjectType.Fire },
            [3] = new HashSet<GameObjectType> { GameObjectType.Strawberry, GameObjectType.Blueberry, GameObjectType.Shield, GameObjectType.Fire, GameObjectType.Bomb }
        };
        foreach (int level in new[] { 1, 2, 3 })
        {
            SnakeGame game = _driver.CreateGame(level, GamePhase.Playing);
            game.SetTestRandom(new FixedRandom());
            game.MaintainLevelObjects();
            Assert.Equal(allowedByLevel[level], game.Objects.Values.ToHashSet());
            _driver.AssertObjectsAreLegal(game);
        }

        SnakeGame shieldNotYetCollected = new SnakeGame();
        shieldNotYetCollected.SetTestScenario(2, GamePhase.Playing, shieldPickupsSpawned: 1, shieldPickupsCollected: 0);
        shieldNotYetCollected.MaintainLevelObjects();
        Assert.DoesNotContain(GameObjectType.Shield, shieldNotYetCollected.Objects.Values);

        SnakeGame secondShield = new SnakeGame();
        secondShield.SetTestScenario(2, GamePhase.Playing, shieldPickupsSpawned: 1, shieldPickupsCollected: 1);
        secondShield.MaintainLevelObjects();
        Assert.Contains(GameObjectType.Shield, secondShield.Objects.Values);
        secondShield.SetTestScenario(2, GamePhase.Playing, shieldPickupsSpawned: 2, shieldPickupsCollected: 2);
        secondShield.MaintainLevelObjects();
        Assert.DoesNotContain(GameObjectType.Shield, secondShield.Objects.Values);

        List<Point> fullBoard = new List<Point>();
        for (int y = 0; y < 10; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                fullBoard.Add(new Point(x, y));
            }
        }

        SnakeGame noRoom = new SnakeGame();
        noRoom.SetTestScenario(1, GamePhase.Playing, fullBoard.Count, fullBoard, direction: Direction.Right);
        noRoom.MaintainLevelObjects();
        Assert.Empty(noRoom.Objects);
    }

    [Fact]
    public void T15_ShowGameOverScreen_ShowsDeathResultAndRequiresDeathPending()
    {
        SnakeGame game = _driver.CreateGame(1, GamePhase.DeathPending, snakeLength: 3);
        game.SetTestScenario(1, GamePhase.DeathPending, snakeLength: 3);
        game.ShowGameOverScreen();
        Assert.Equal(GamePhase.GameOver, game.Phase);
        Assert.Equal(1, game.FailedLevel);
        Assert.Equal(3, game.FinalLength);

        SnakeGame wrongPhase = _driver.CreateGame(1, GamePhase.Playing);
        var previousSnake = wrongPhase.Snake.ToArray();
        Assert.Throws<InvalidOperationException>(wrongPhase.ShowGameOverScreen);
        Assert.Equal(previousSnake, wrongPhase.Snake);
        Assert.Equal(GamePhase.Playing, wrongPhase.Phase);
    }

    [Fact]
    public void T16_ShowThirdLevelSuccessScreen_Accepts50OrMoreAndRejectsIncompleteRun()
    {
        foreach (int length in new[] { 50, 51 })
        {
            SnakeGame game = new SnakeGame();
            game.SetTestScenario(3, GamePhase.SuccessPending, length);
            game.ShowThirdLevelSuccessScreen();
            Assert.Equal(GamePhase.Success, game.Phase);
            Assert.Equal(100, game.ProgressPercent);
            Assert.Equal(length, game.SnakeLength);
        }

        SnakeGame incomplete = new SnakeGame();
        incomplete.SetTestScenario(3, GamePhase.Playing, 49);
        var previousSnake = incomplete.Snake.ToArray();
        Assert.Throws<InvalidOperationException>(incomplete.ShowThirdLevelSuccessScreen);
        Assert.Equal(previousSnake, incomplete.Snake);
        Assert.Equal(GamePhase.Playing, incomplete.Phase);
    }

    [Fact]
    public void T17_RestartGame_ClearsHistoryAndStartsFirstLevelImmediatelyOnlyAfterResult()
    {
        foreach (GamePhase resultPhase in new[] { GamePhase.GameOver, GamePhase.Success })
        {
            SnakeGame game = new SnakeGame();
            game.SetTestScenario(resultPhase == GamePhase.Success ? 3 : 2, resultPhase, resultPhase == GamePhase.Success ? 50 : 4, shieldCount: 1, lastCompletedLevel: 1, lastCompletedLength: 50);
            game.RestartGame();
            Assert.Equal(1, game.Level);
            Assert.Equal(GamePhase.Playing, game.Phase);
            Assert.Equal(3, game.SnakeLength);
            Assert.Equal(15, game.ProgressPercent);
            Assert.Equal(0, game.ShieldCount);
            Assert.Equal(0, game.LastCompletedLevel);
            Assert.Contains(GameObjectType.Strawberry, game.Objects.Values);
            Assert.DoesNotContain(GameObjectType.Shield, game.Objects.Values);
        }

        SnakeGame active = _driver.CreateGame(1, GamePhase.Playing);
        var previousSnake = active.Snake.ToArray();
        active.RestartGame();
        Assert.Equal(previousSnake, active.Snake);
        Assert.Equal(GamePhase.Playing, active.Phase);
    }

    private SnakeGame CreateNextObjectGame(int level, int length, GameObjectType objectType, int shieldCount = 0, int shieldPickupsSpawned = 0, int shieldPickupsCollected = 0)
    {
        SnakeGame game = new SnakeGame();
        game.SetTestScenario(level, GamePhase.Playing, snakeLength: length);
        List<Point> snake = _driver.SnakeOnSafePath(game, length);
        Direction direction = _driver.DirectionAlongSafePath(game, length);
        Point target = game.SafePath[length];
        Dictionary<Point, GameObjectType> objects = new Dictionary<Point, GameObjectType> { [target] = objectType };
        game.SetTestScenario(level, GamePhase.Playing, length, snake, objects, shieldCount, shieldPickupsSpawned, shieldPickupsCollected, direction);
        game.SetTestRandom(new FixedRandom());
        return game;
    }
}
