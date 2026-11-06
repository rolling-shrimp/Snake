using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class SnakeGameApplicationServiceTests
{
    [Fact]
    public void LoadSnapshot_DoesNotExposeWorkingChangesBeforeCommit()
    {
        SnakeGameSession session = new SnakeGameSession();

        VersionedSnakeGameSnapshot workingCopy = session.Load();
        Assert.True(workingCopy.Snapshot.Game.StartLevel());

        VersionedSnakeGameSnapshot unchanged = session.Load();
        Assert.Equal(GamePhase.Ready, unchanged.Snapshot.Game.Phase);

        Assert.True(session.TryCommit(workingCopy.Version, workingCopy.Snapshot));
        Assert.Equal(GamePhase.Playing, session.Load().Snapshot.Game.Phase);
        Assert.False(session.TryCommit(workingCopy.Version, workingCopy.Snapshot));
    }

    [Fact]
    public void GetCurrentScreen_ReturnsCompleteFirstLevelReadyScreenWithoutMutation()
    {
        SnakeGameSession session = new SnakeGameSession();
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());
        long versionBefore = session.Load().Version;

        GetCurrentScreenResult result = service.GetCurrentScreen(new GetCurrentScreenQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(GetCurrentScreenOutcome.ScreenReturned, result.Outcome);
        Assert.Equal(RuntimeDirective.Unchanged, result.RuntimeDirective);
        Assert.NotNull(result.Screen);
        Assert.Equal(GameScreenKind.FirstLevelReady, result.Screen.ScreenKind);
        Assert.Equal(100, result.Screen.BoardCells.Count);
        Assert.Equal(3, result.Screen.SnakeBody.Count);
        Assert.Empty(result.Screen.Objects);
        Assert.Equal(3, result.Screen.LevelStatus.CurrentLength);
        Assert.Equal(20, result.Screen.LevelStatus.TargetLength);
        Assert.Equal(15, result.Screen.LevelStatus.ProgressPercentage);
        Assert.Contains(GameAction.Start, result.Screen.AvailableActions);
        Assert.Equal(versionBefore, session.Load().Version);
    }

    [Fact]
    public void StartCurrentLevel_CommitsFirstLevelAndReturnsOnePlayingScreen()
    {
        SnakeGameSession session = new SnakeGameSession();
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());

        StartCurrentLevelResult result = service.StartCurrentLevel(new StartCurrentLevelCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(StartCurrentLevelOutcome.Started, result.Outcome);
        Assert.Equal(RuntimeDirective.StartOrReset, result.RuntimeDirective);
        Assert.NotNull(result.Screen);
        Assert.Equal(GameScreenKind.FirstLevelPlaying, result.Screen.ScreenKind);
        Assert.Single(result.Screen.Objects);
        Assert.Equal(BoardObjectKind.OrdinaryFood, result.Screen.Objects[0].Kind);
        Assert.Equal(GamePhase.Playing, session.Load().Snapshot.Game.Phase);
        Assert.Equal(0, session.Load().Snapshot.Game.LastProcessedElapsedMilliseconds);
    }

    [Fact]
    public void StartCurrentLevel_RollsBackWhenInitialObjectPositionIsUnavailable()
    {
        SnakeGameSession session = new SnakeGameSession();
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new NoSpawnPositionSelector());
        long versionBefore = session.Load().Version;

        StartCurrentLevelResult result = service.StartCurrentLevel(new StartCurrentLevelCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("InitialSpawnUnavailable", result.FailureCode);
        Assert.Equal(RuntimeDirective.Unchanged, result.RuntimeDirective);
        Assert.Equal(versionBefore, session.Load().Version);
        Assert.Equal(GamePhase.Ready, session.Load().Snapshot.Game.Phase);
        Assert.Empty(session.Load().Snapshot.Food.ActiveItems);
    }

    [Fact]
    public void StartCurrentLevel_DoesNotExposePartiallySpawnedWorkingSnapshot()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot ready = session.Load();
        Assert.True(ready.Snapshot.Level.Advance());
        Assert.True(session.TryCommit(ready.Version, ready.Snapshot));
        long versionBefore = session.Load().Version;
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new NoSpawnPositionSelector(1));

        StartCurrentLevelResult result = service.StartCurrentLevel(new StartCurrentLevelCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal("InitialSpawnUnavailable", result.FailureCode);
        Assert.NotNull(result.Screen);
        Assert.Empty(result.Screen.Objects);
        Assert.Equal(GameScreenKind.SecondLevelReady, result.Screen.ScreenKind);
        Assert.Equal(versionBefore, session.Load().Version);
        Assert.Empty(session.Load().Snapshot.Food.ActiveItems);
    }

    [Fact]
    public void StartCurrentLevel_SecondLevelSpawnsFourUniqueObjects()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot ready = session.Load();
        Assert.True(ready.Snapshot.Level.Advance());
        ready.Snapshot.GameResult.Record(
            new LevelResultId(Guid.NewGuid()),
            LevelNumber.First,
            LevelOutcome.Passed,
            20,
            null);
        Assert.True(session.TryCommit(ready.Version, ready.Snapshot));
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());

        StartCurrentLevelResult result = service.StartCurrentLevel(new StartCurrentLevelCommand());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Screen);
        Assert.Null(result.Screen.ResultCard);
        Assert.Equal(4, result.Screen.Objects.Count);
        Assert.Equal(4, result.Screen.Objects.Select(item => item.Position).DistinctBy(
            position => (position.X, position.Y)).Count());
        Assert.Equal(
            new List<BoardObjectKind>
            {
                BoardObjectKind.OrdinaryFood,
                BoardObjectKind.SpecialFood,
                BoardObjectKind.Shield,
                BoardObjectKind.Fire
            },
            result.Screen.Objects.Select(item => item.Kind).ToList());
    }

    [Fact]
    public void SetSnakeDirection_AcceptsNonOppositeDirectionWithoutMovingSnake()
    {
        SnakeGameSession session = new SnakeGameSession();
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());
        service.StartCurrentLevel(new StartCurrentLevelCommand());
        List<GridPosition> bodyBefore = session.Load().Snapshot.Snake.BodyCells;

        SetSnakeDirectionResult result = service.SetSnakeDirection(
            new SetSnakeDirectionCommand(Snake.UseCases.Direction.Up));

        Assert.True(result.IsSuccess);
        Assert.Equal(SetSnakeDirectionOutcome.Accepted, result.Outcome);
        Assert.Equal(RuntimeDirective.Unchanged, result.RuntimeDirective);
        Assert.Equal(bodyBefore, session.Load().Snapshot.Snake.BodyCells);
        Assert.Equal(Snake.Entities.Direction.Up, session.Load().Snapshot.Snake.QueuedDirection);
    }

    [Fact]
    public void ProcessGameTick_ConsumesQueuedDirectionOnNextDueMovement()
    {
        SnakeGameSession session = new SnakeGameSession();
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());
        service.StartCurrentLevel(new StartCurrentLevelCommand());
        SetSnakeDirectionResult directionResult = service.SetSnakeDirection(
            new SetSnakeDirectionCommand(Snake.UseCases.Direction.Down));

        ProcessGameTickResult tickResult = service.ProcessGameTick(
            new ProcessGameTickCommand(250));

        Assert.True(directionResult.IsSuccess);
        Assert.Equal(SetSnakeDirectionOutcome.Accepted, directionResult.Outcome);
        Assert.True(tickResult.IsSuccess, tickResult.FailureMessage);
        Assert.Equal(new GridPositionDTO(3, 2), tickResult.Screen!.SnakeBody[0]);
        Assert.Equal(Snake.Entities.Direction.Down, session.Load().Snapshot.Snake.CurrentDirection);
        Assert.Null(session.Load().Snapshot.Snake.QueuedDirection);
    }

    [Fact]
    public void RestartGame_ReplacesTerminalRoundWithFirstLevelPlayingSnapshot()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot terminal = session.Load();
        Assert.True(terminal.Snapshot.Game.StartLevel());
        terminal.Snapshot.Game.EndGame();
        terminal.Snapshot.Food.SpawnSpecial(
            new FoodItemId(Guid.NewGuid()),
            new GridPosition(8, 8));
        terminal.Snapshot.Shield.Spawn(
            new ShieldItemId(Guid.NewGuid()),
            new GridPosition(7, 8));
        Assert.True(session.TryCommit(terminal.Version, terminal.Snapshot));
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());

        RestartGameResult result = service.RestartGame(new RestartGameCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(RestartGameOutcome.Restarted, result.Outcome);
        Assert.Equal(RuntimeDirective.StartOrReset, result.RuntimeDirective);
        Assert.NotNull(result.Screen);
        Assert.Equal(GameScreenKind.FirstLevelPlaying, result.Screen.ScreenKind);
        Assert.Equal(3, result.Screen.SnakeBody.Count);
        Assert.Single(result.Screen.Objects);
        Assert.Equal(BoardObjectKind.OrdinaryFood, result.Screen.Objects[0].Kind);
        Assert.Equal(0, result.Screen.LevelStatus.RemainingShieldCharges);
    }

    [Fact]
    public void ProcessGameTick_CatchesUpEveryMovementDueBeforeAccumulatedTime()
    {
        SnakeGameSession session = new SnakeGameSession();
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());
        service.StartCurrentLevel(new StartCurrentLevelCommand());

        ProcessGameTickResult result = service.ProcessGameTick(new ProcessGameTickCommand(1000));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProcessGameTickOutcome.EventsProcessed, result.Outcome);
        Assert.Equal(4, result.ProcessedEvents.Count(item =>
            item.EventType == GameTickEventType.SnakeMoved));
        Assert.Equal(new List<long> { 250, 500, 750, 1000 }, result.ProcessedEvents
            .Where(item => item.EventType == GameTickEventType.SnakeMoved)
            .Select(item => item.DueElapsedMilliseconds)
            .ToList());
        Assert.NotNull(result.Screen);
        Assert.Equal(new GridPositionDTO(7, 1), result.Screen.SnakeBody[0]);
        Assert.Equal(1000, session.Load().Snapshot.Game.LastProcessedElapsedMilliseconds);
    }

    [Fact]
    public void ProcessGameTick_AllowsNormalMoveIntoDepartingTailCell()
    {
        SnakeGameSession session = new SnakeGameSession();
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());
        service.StartCurrentLevel(new StartCurrentLevelCommand());
        VersionedSnakeGameSnapshot setup = session.Load();
        setup.Snapshot.Snake.ResetSafely(
            new List<GridPosition>
            {
                new GridPosition(1, 1),
                new GridPosition(1, 2),
                new GridPosition(2, 2),
                new GridPosition(2, 1)
            },
            setup.Snapshot.Board,
            BoundaryPolicy.Wrap);
        Assert.True(session.TryCommit(setup.Version, setup.Snapshot));

        ProcessGameTickResult result = service.ProcessGameTick(new ProcessGameTickCommand(250));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProcessGameTickOutcome.EventsProcessed, result.Outcome);
        Assert.NotNull(result.Screen);
        Assert.Equal(
            new List<GridPositionDTO>
            {
                new GridPositionDTO(2, 1),
                new GridPositionDTO(1, 1),
                new GridPositionDTO(1, 2),
                new GridPositionDTO(2, 2)
            },
            result.Screen.SnakeBody);
    }

    [Fact]
    public void ProcessGameTick_RollsBackSpecialFoodWhenNoTailExtensionIsLegal()
    {
        SnakeGameSession session = CreateSecondLevelPlayingSpecialFoodTrap();
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());
        VersionedSnakeGameSnapshot before = session.Load();

        ProcessGameTickResult result = service.ProcessGameTick(new ProcessGameTickCommand(250));

        Assert.False(result.IsSuccess);
        Assert.Equal("SnakeTopologyUnavailable", result.FailureCode);
        VersionedSnakeGameSnapshot after = session.Load();
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(0, after.Snapshot.Game.LastProcessedElapsedMilliseconds);
        Assert.Single(after.Snapshot.Food.ActiveItems, item => item.Type == FoodType.Special);
        Assert.Equal(before.Snapshot.Snake.BodyCells, after.Snapshot.Snake.BodyCells);
    }

    [Fact]
    public void ProcessGameTick_OrdinaryFoodGrowsOneContiguousCell()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot setup = session.Load();
        Assert.True(setup.Snapshot.Game.StartLevel());
        setup.Snapshot.Food.SpawnOrdinary(
            new FoodItemId(Guid.NewGuid()),
            new GridPosition(4, 1));
        Assert.True(session.TryCommit(setup.Version, setup.Snapshot));
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());

        ProcessGameTickResult result = service.ProcessGameTick(new ProcessGameTickCommand(250));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Screen);
        Assert.Equal(4, result.Screen.SnakeBody.Count);
        Assert.Equal(new GridPositionDTO(4, 1), result.Screen.SnakeBody[0]);
        Assert.True(session.Load().Snapshot.Board.IsContinuous(
            session.Load().Snapshot.Snake.BodyCells,
            BoundaryPolicy.Wrap));
        Assert.Contains(result.ProcessedEvents, item =>
            item.EventType == GameTickEventType.ObjectContact
            && item.Outcome == FoodType.Ordinary.ToString());
    }

    [Fact]
    public void ProcessGameTick_ShieldRescueAndGameOverAreMutuallyExclusive()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot setup = session.Load();
        Assert.True(setup.Snapshot.Game.StartLevel());
        setup.Snapshot.Snake.ResetSafely(
            new List<GridPosition>
            {
                new GridPosition(1, 1),
                new GridPosition(2, 1),
                new GridPosition(2, 2),
                new GridPosition(1, 2)
            },
            setup.Snapshot.Board,
            BoundaryPolicy.Wrap);
        ShieldItemId shieldItemId = new ShieldItemId(Guid.NewGuid());
        setup.Snapshot.Shield.Spawn(shieldItemId, new GridPosition(8, 8));
        Assert.True(setup.Snapshot.Shield.Collect(shieldItemId));
        Assert.True(session.TryCommit(setup.Version, setup.Snapshot));
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());

        ProcessGameTickResult result = service.ProcessGameTick(new ProcessGameTickCommand(250));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProcessGameTickOutcome.ShieldRescued, result.Outcome);
        Assert.DoesNotContain(result.ProcessedEvents, item =>
            item.EventType == GameTickEventType.GameOver);
        VersionedSnakeGameSnapshot rescued = session.Load();
        Assert.Equal(GamePhase.Playing, rescued.Snapshot.Game.Phase);
        Assert.Equal(0, rescued.Snapshot.Shield.RemainingCharges);
        Assert.Equal(4, rescued.Snapshot.Snake.Length);
        Assert.True(rescued.Snapshot.Board.IsContinuous(
            rescued.Snapshot.Snake.BodyCells,
            BoundaryPolicy.Wrap));
    }

    [Fact]
    public void ProcessGameTick_UnrescuedBombStopsWithoutReplenishingObject()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot setup = session.Load();
        Assert.True(setup.Snapshot.Game.StartLevel());
        setup.Snapshot.Bomb.Spawn(
            new BombItemId(Guid.NewGuid()),
            new GridPosition(4, 1));
        Assert.True(session.TryCommit(setup.Version, setup.Snapshot));
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());

        ProcessGameTickResult result = service.ProcessGameTick(new ProcessGameTickCommand(250));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProcessGameTickOutcome.GameOver, result.Outcome);
        Assert.Equal(RuntimeDirective.Stop, result.RuntimeDirective);
        Assert.DoesNotContain(result.ProcessedEvents, item =>
            item.EventType == GameTickEventType.ObjectReplenished);
        Assert.Equal(GamePhase.GameOver, session.Load().Snapshot.Game.Phase);
        Assert.Empty(session.Load().Snapshot.Bomb.GetOccupiedCells());
    }

    [Fact]
    public void ProcessGameTick_LevelPassStopsReplenishmentAndReturnsNextReadyScreen()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot setup = session.Load();
        Assert.True(setup.Snapshot.Level.Advance());
        Assert.True(setup.Snapshot.Game.StartLevel());
        List<GridPosition> body = setup.Snapshot.Board.FindSafeBody(
            19,
            BoundaryPolicy.Wrap,
            new List<GridPosition>())
            ?? throw new InvalidOperationException("測試無法建立十九格蛇身。");
        setup.Snapshot.Snake.ResetSafely(body, setup.Snapshot.Board, BoundaryPolicy.Wrap);
        GridPosition nextHead = setup.Snapshot.Board.ResolveMovement(
            body[0],
            Snake.Entities.Direction.Right,
            BoundaryPolicy.Wrap).Position
            ?? throw new InvalidOperationException("測試蛇頭右側應有合法格位。");
        setup.Snapshot.Food.SpawnOrdinary(new FoodItemId(Guid.NewGuid()), nextHead);
        Assert.True(session.TryCommit(setup.Version, setup.Snapshot));
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());

        ProcessGameTickResult result = service.ProcessGameTick(new ProcessGameTickCommand(250));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProcessGameTickOutcome.LevelReady, result.Outcome);
        Assert.Equal(RuntimeDirective.Stop, result.RuntimeDirective);
        Assert.NotNull(result.Screen);
        Assert.Equal(GameScreenKind.ThirdLevelReady, result.Screen.ScreenKind);
        Assert.Equal(20, result.Screen.ResultCard?.FinalLength);
        Assert.DoesNotContain(result.ProcessedEvents, item =>
            item.EventType == GameTickEventType.ObjectReplenished);
    }

    [Fact]
    public void ProcessGameTick_ThirdLevelSpecialFoodCompletesAtLengthFiftyOne()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot setup = session.Load();
        Assert.True(setup.Snapshot.Level.Advance());
        Assert.True(setup.Snapshot.Level.Advance());
        setup.Snapshot.DoorAndMaze.PrepareThirdLevel(new DoorId(Guid.NewGuid()));
        Assert.True(setup.Snapshot.Game.StartLevel());
        List<GridPosition> blockedCells = setup.Snapshot.DoorAndMaze.GetUnavailableCells();
        List<GridPosition> body = setup.Snapshot.Board.FindSafeBody(
            49,
            BoundaryPolicy.Blocked,
            blockedCells)
            ?? throw new InvalidOperationException("測試無法建立四十九格安全蛇身。");
        setup.Snapshot.Snake.ResetSafely(body, setup.Snapshot.Board, BoundaryPolicy.Blocked);
        GridPosition nextHead = setup.Snapshot.Board.ResolveMovement(
            body[0],
            Snake.Entities.Direction.Right,
            BoundaryPolicy.Blocked).Position
            ?? throw new InvalidOperationException("測試蛇頭右側應有合法格位。");
        setup.Snapshot.Food.SpawnSpecial(new FoodItemId(Guid.NewGuid()), nextHead);
        Assert.True(session.TryCommit(setup.Version, setup.Snapshot));
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());

        ProcessGameTickResult result = service.ProcessGameTick(new ProcessGameTickCommand(250));

        Assert.True(result.IsSuccess, result.FailureMessage);
        Assert.Equal(ProcessGameTickOutcome.GameCompleted, result.Outcome);
        Assert.Equal(RuntimeDirective.Stop, result.RuntimeDirective);
        Assert.NotNull(result.Screen);
        Assert.Equal(GameScreenKind.GameCompleted, result.Screen.ScreenKind);
        Assert.Equal(51, result.Screen.LevelStatus.CurrentLength);
        Assert.Equal(100, result.Screen.LevelStatus.ProgressPercentage);
        Assert.Equal(51, result.Screen.ResultCard?.FinalLength);
        Assert.DoesNotContain(result.ProcessedEvents, item =>
            item.EventType == GameTickEventType.ObjectReplenished);
    }

    [Fact]
    public void ProcessGameTick_DoorEventsPrecedeMovementAndDeferredCloseFinishesAfterDeparture()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot setup = session.Load();
        Assert.True(setup.Snapshot.Level.Advance());
        Assert.True(setup.Snapshot.Level.Advance());
        setup.Snapshot.DoorAndMaze.PrepareThirdLevel(new DoorId(Guid.NewGuid()));
        Assert.True(setup.Snapshot.Game.StartLevel());
        setup.Snapshot.Game.CommitTickTime(5750);
        setup.Snapshot.Snake.ResetSafely(
            new List<GridPosition>
            {
                new GridPosition(3, 4),
                new GridPosition(3, 5),
                new GridPosition(4, 5),
                new GridPosition(5, 5)
            },
            setup.Snapshot.Board,
            BoundaryPolicy.Blocked);
        Assert.True(session.TryCommit(setup.Version, setup.Snapshot));
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());

        ProcessGameTickResult result = service.ProcessGameTick(new ProcessGameTickCommand(6000));

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new List<GameTickEventType>
            {
                GameTickEventType.DoorCloseDeferred,
                GameTickEventType.SnakeMoved,
                GameTickEventType.DoorClosedAfterDeparture
            },
            result.ProcessedEvents.Select(item => item.EventType).ToList());
        Assert.NotNull(result.Screen);
        Assert.Equal(DoorVisualState.Closed, Assert.Single(result.Screen.Doors).State);
    }

    [Fact]
    public void ProcessGameTick_TimeRegressionRollsBackWholeTick()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot setup = session.Load();
        Assert.True(setup.Snapshot.Game.StartLevel());
        setup.Snapshot.Game.CommitTickTime(1000);
        Assert.True(session.TryCommit(setup.Version, setup.Snapshot));
        long versionBefore = session.Load().Version;
        SnakeGameApplicationService service = new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());

        ProcessGameTickResult result = service.ProcessGameTick(new ProcessGameTickCommand(999));

        Assert.False(result.IsSuccess);
        Assert.Equal("InvalidElapsedTime", result.FailureCode);
        Assert.Empty(result.ProcessedEvents);
        Assert.Equal(versionBefore, session.Load().Version);
        Assert.Equal(1000, session.Load().Snapshot.Game.LastProcessedElapsedMilliseconds);
    }

    private SnakeGameSession CreateSecondLevelPlayingSpecialFoodTrap()
    {
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot setup = session.Load();
        Assert.True(setup.Snapshot.Level.Advance());
        Assert.True(setup.Snapshot.Game.StartLevel());
        setup.Snapshot.Snake.ResetSafely(
            new List<GridPosition>
            {
                new GridPosition(4, 4),
                new GridPosition(3, 4),
                new GridPosition(2, 4)
            },
            setup.Snapshot.Board,
            BoundaryPolicy.Wrap);
        setup.Snapshot.Food.SpawnSpecial(
            new FoodItemId(Guid.NewGuid()),
            new GridPosition(5, 4));
        setup.Snapshot.Food.SpawnOrdinary(
            new FoodItemId(Guid.NewGuid()),
            new GridPosition(1, 4));
        setup.Snapshot.Shield.Spawn(
            new ShieldItemId(Guid.NewGuid()),
            new GridPosition(2, 5));
        setup.Snapshot.Fire.Spawn(
            new FireItemId(Guid.NewGuid()),
            new GridPosition(2, 3));
        Assert.True(session.TryCommit(setup.Version, setup.Snapshot));
        return session;
    }
}
