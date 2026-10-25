using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class ShowThirdLevelInformationAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-THIRD-LEVEL-READY-BOARD-001")]
    public void SCN_SHOW_THIRD_LEVEL_READY_BOARD_001_ReturnsMazeAndSafeSnake()
    {
        SnakeGameSession session = CreateThirdLevelReadySession();
        ShowThirdLevelReadyBoardUseCase useCase = new ShowThirdLevelReadyBoardUseCase(session);

        ShowThirdLevelReadyBoardResult result = useCase.Execute(new ShowThirdLevelReadyBoardQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(10, result.Data.BoardRows);
        Assert.Equal(10, result.Data.BoardColumns);
        Assert.Equal(3, result.Data.SnakeCells.Count);
        Assert.NotEmpty(result.Data.WallCells);
        Assert.NotEmpty(result.Data.Doors);
        Assert.True(result.Data.IsStartButtonVisible);
        Assert.All(result.Data.SnakeCells, cell =>
        {
            Assert.DoesNotContain(result.Data.WallCells, wall => wall.X == cell.X && wall.Y == cell.Y);
            Assert.DoesNotContain(result.Data.Doors, door => door.Position.X == cell.X && door.Position.Y == cell.Y);
        });
        Assert.True(CountReachableCells(result.Data) >= 50);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-THIRD-LEVEL-READY-BOARD-002")]
    public void SCN_SHOW_THIRD_LEVEL_READY_BOARD_002_RejectsUnavailableStateWithoutData()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        ShowThirdLevelReadyBoardUseCase useCase = new ShowThirdLevelReadyBoardUseCase(session);

        ShowThirdLevelReadyBoardResult result = useCase.Execute(new ShowThirdLevelReadyBoardQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_GAME_PHASE", result.FailureCode);
        Assert.Equal("目前不是第三關待開始階段。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-THIRD-LEVEL-RULES-001")]
    public void SCN_SHOW_THIRD_LEVEL_RULES_001_ReturnsSixOrderedRules()
    {
        SnakeGameSession session = CreateThirdLevelReadySession();
        ShowThirdLevelRulesUseCase useCase = new ShowThirdLevelRulesUseCase(session);

        ShowThirdLevelRulesResult result = useCase.Execute(new ShowThirdLevelRulesQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Collection(
            result.Data.RuleHints,
            rule => Assert.Equal(("ORDINARY_FOOD_GROWTH", "普通食物 +1 格"), (rule.Code, rule.Text)),
            rule => Assert.Equal(("SPECIAL_FOOD_GROWTH", "特殊食物 +2 格"), (rule.Code, rule.Text)),
            rule => Assert.Equal(("SHIELD_RESCUE", "護盾 +1"), (rule.Code, rule.Text)),
            rule => Assert.Equal(("FIRE_SHRINK", "火焰 −2 格"), (rule.Code, rule.Text)),
            rule => Assert.Equal(("BOMB_FATAL", "炸彈致命"), (rule.Code, rule.Text)),
            rule => Assert.Equal(("WALL_BLOCKED", "不能穿牆"), (rule.Code, rule.Text)));
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-THIRD-LEVEL-RULES-002")]
    public void SCN_SHOW_THIRD_LEVEL_RULES_002_RejectsUnavailableStateWithoutData()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third);
        ShowThirdLevelRulesUseCase useCase = new ShowThirdLevelRulesUseCase(session);

        ShowThirdLevelRulesResult result = useCase.Execute(new ShowThirdLevelRulesQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_GAME_PHASE", result.FailureCode);
        Assert.Equal("目前不是第三關待開始階段。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-THIRD-LEVEL-STATUS-001")]
    public void SCN_SHOW_THIRD_LEVEL_STATUS_001_ReturnsThirdLevelProgress()
    {
        SnakeGameSession session = CreateThirdLevelReadySession();
        ShowThirdLevelStatusUseCase useCase = new ShowThirdLevelStatusUseCase(session);

        ShowThirdLevelStatusResult result = useCase.Execute(new ShowThirdLevelStatusQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("第三關", result.Data.LevelName);
        Assert.Equal(3, result.Data.CurrentLength);
        Assert.Equal(50, result.Data.TargetLength);
        Assert.Equal(6, result.Data.ProgressPercent);
        Assert.Equal(0, result.Data.ShieldRemainingCharges);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-THIRD-LEVEL-STATUS-002")]
    public void SCN_SHOW_THIRD_LEVEL_STATUS_002_RejectsUnavailableStateWithoutData()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        ShowThirdLevelStatusUseCase useCase = new ShowThirdLevelStatusUseCase(session);

        ShowThirdLevelStatusResult result = useCase.Execute(new ShowThirdLevelStatusQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_GAME_PHASE", result.FailureCode);
        Assert.Equal("目前不是第三關待開始階段。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-THIRD-LEVEL-DOOR-STATUS-001")]
    public void SCN_SHOW_THIRD_LEVEL_DOOR_STATUS_001_ReturnsCurrentDoorStates()
    {
        SnakeGameSession session = CreateThirdLevelReadySession();
        Door openDoor = session.DoorAndMaze.Doors.Single();
        Door warningDoor = AddDoor(session, new GridPosition(2, 2), DoorPhase.ClosingWarning);
        Door deferredDoor = AddDoor(session, new GridPosition(3, 3), DoorPhase.CloseDeferred);
        Door closedDoor = AddDoor(session, new GridPosition(4, 4), DoorPhase.Closed);
        session.Game.ActivateLevel();
        ShowThirdLevelDoorStatusUseCase useCase = new ShowThirdLevelDoorStatusUseCase(session);

        ShowThirdLevelDoorStatusResult result = useCase.Execute(new ShowThirdLevelDoorStatusQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.IsCycleActive);
        AssertDoor(result.Data.Doors, openDoor, "Open");
        AssertDoor(result.Data.Doors, warningDoor, "ClosingWarning");
        AssertDoor(result.Data.Doors, deferredDoor, "CloseDeferred");
        AssertDoor(result.Data.Doors, closedDoor, "Closed");
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-THIRD-LEVEL-DOOR-STATUS-002")]
    public void SCN_SHOW_THIRD_LEVEL_DOOR_STATUS_002_RejectsUnavailableStateWithoutData()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        ShowThirdLevelDoorStatusUseCase useCase = new ShowThirdLevelDoorStatusUseCase(session);

        ShowThirdLevelDoorStatusResult result = useCase.Execute(new ShowThirdLevelDoorStatusQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_GAME_PHASE", result.FailureCode);
        Assert.Equal("目前沒有可顯示的第三關門狀態。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    private SnakeGameSession CreateThirdLevelReadySession()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Third);
        session.DoorAndMaze.PrepareThirdLevel(new DoorId(Guid.NewGuid()));
        return session;
    }

    private Door AddDoor(SnakeGameSession session, GridPosition position, DoorPhase phase)
    {
        DoorId doorId = new DoorId(Guid.NewGuid());
        session.DoorAndMaze.AddDoor(doorId, position);
        Door door = session.DoorAndMaze.FindDoor(doorId)
            ?? throw new InvalidOperationException("測試門建立失敗。");
        door.ChangePhase(phase);
        return door;
    }

    private void AssertDoor(List<DoorStatusDTO> doorStatuses, Door expectedDoor, string expectedState)
    {
        DoorStatusDTO doorStatus = Assert.Single(
            doorStatuses,
            door => door.DoorId == expectedDoor.DoorId.Value);
        Assert.Equal(expectedDoor.Position.X, doorStatus.Position.X);
        Assert.Equal(expectedDoor.Position.Y, doorStatus.Position.Y);
        Assert.Equal(expectedState, doorStatus.State);
    }

    private int CountReachableCells(ThirdLevelReadyBoardRspDTO board)
    {
        HashSet<(int X, int Y)> blockedCells = board.WallCells
            .Select(cell => (cell.X, cell.Y))
            .ToHashSet();
        foreach (DoorStatusDTO door in board.Doors.Where(door => door.State == "Closed"))
        {
            blockedCells.Add((door.Position.X, door.Position.Y));
        }

        Queue<(int X, int Y)> pendingCells = new Queue<(int X, int Y)>();
        HashSet<(int X, int Y)> reachableCells = new HashSet<(int X, int Y)>();
        (int X, int Y) start = (board.SnakeCells[0].X, board.SnakeCells[0].Y);
        pendingCells.Enqueue(start);
        reachableCells.Add(start);
        while (pendingCells.Count > 0)
        {
            (int X, int Y) current = pendingCells.Dequeue();
            List<(int X, int Y)> neighbors = new List<(int X, int Y)>
            {
                (current.X + 1, current.Y),
                (current.X - 1, current.Y),
                (current.X, current.Y + 1),
                (current.X, current.Y - 1)
            };
            foreach ((int X, int Y) neighbor in neighbors)
            {
                bool isInsideBoard = neighbor.X >= 0 && neighbor.X < board.BoardColumns
                    && neighbor.Y >= 0 && neighbor.Y < board.BoardRows;
                if (isInsideBoard && !blockedCells.Contains(neighbor) && reachableCells.Add(neighbor))
                {
                    pendingCells.Enqueue(neighbor);
                }
            }
        }

        return reachableCells.Count;
    }
}
