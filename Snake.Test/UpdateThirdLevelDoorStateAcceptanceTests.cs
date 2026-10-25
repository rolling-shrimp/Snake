using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class UpdateThirdLevelDoorStateAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-UPDATE-THIRD-LEVEL-DOOR-STATE-001")]
    public void SCN_UPDATE_THIRD_LEVEL_DOOR_STATE_001_EntersPassableClosingWarningAtFourSeconds()
    {
        SnakeGameSession session = CreateThirdLevelSessionWithDoor(new GridPosition(9, 9));
        UpdateThirdLevelDoorStateUseCase useCase = new UpdateThirdLevelDoorStateUseCase(session);

        UpdateThirdLevelDoorStateResult result = useCase.Execute(
            new UpdateThirdLevelDoorStateCommand(4000));

        Assert.True(result.IsSuccess);
        Door door = Assert.Single(session.DoorAndMaze.Doors);
        Assert.Equal(DoorPhase.ClosingWarning, door.Phase);
        Assert.True(door.IsPassable);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-UPDATE-THIRD-LEVEL-DOOR-STATE-002")]
    public void SCN_UPDATE_THIRD_LEVEL_DOOR_STATE_002_ClosesUnoccupiedDoorAtSixSeconds()
    {
        SnakeGameSession session = CreateThirdLevelSessionWithDoor(new GridPosition(9, 9));
        UpdateThirdLevelDoorStateUseCase useCase = new UpdateThirdLevelDoorStateUseCase(session);

        UpdateThirdLevelDoorStateResult result = useCase.Execute(
            new UpdateThirdLevelDoorStateCommand(6000));

        Assert.True(result.IsSuccess);
        Assert.Equal(DoorPhase.Closed, Assert.Single(session.DoorAndMaze.Doors).Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-UPDATE-THIRD-LEVEL-DOOR-STATE-003")]
    public void SCN_UPDATE_THIRD_LEVEL_DOOR_STATE_003_DefersUntilEntireSnakeLeavesDoorCell()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third);
        GridPosition occupiedDoorPosition = session.Snake.BodyCells[1];
        session.DoorAndMaze.AddDoor(new DoorId(Guid.NewGuid()), occupiedDoorPosition);
        UpdateThirdLevelDoorStateUseCase useCase = new UpdateThirdLevelDoorStateUseCase(session);

        useCase.Execute(new UpdateThirdLevelDoorStateCommand(6000));

        Assert.Equal(DoorPhase.CloseDeferred, Assert.Single(session.DoorAndMaze.Doors).Phase);
        Assert.True(Assert.Single(session.DoorAndMaze.Doors).IsPassable);

        session.Snake.Reset(new List<GridPosition>
        {
            new GridPosition(7, 7),
            new GridPosition(6, 7),
            new GridPosition(5, 7)
        });
        useCase.Execute(new UpdateThirdLevelDoorStateCommand(6001));

        Assert.Equal(DoorPhase.Closed, Assert.Single(session.DoorAndMaze.Doors).Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-UPDATE-THIRD-LEVEL-DOOR-STATE-004")]
    public void SCN_UPDATE_THIRD_LEVEL_DOOR_STATE_004_IgnoresNonThirdOrNonPlayingGame()
    {
        SnakeGameSession secondLevelSession = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second);
        secondLevelSession.DoorAndMaze.AddDoor(new DoorId(Guid.NewGuid()), new GridPosition(9, 9));
        UpdateThirdLevelDoorStateUseCase secondLevelUseCase = new UpdateThirdLevelDoorStateUseCase(secondLevelSession);

        UpdateThirdLevelDoorStateResult result = secondLevelUseCase.Execute(
            new UpdateThirdLevelDoorStateCommand(6000));

        Assert.True(result.IsSuccess);
        Assert.Equal(DoorPhase.Open, Assert.Single(secondLevelSession.DoorAndMaze.Doors).Phase);

        SnakeGameSession readySession = new GameTestSessionFactory().CreateReady(LevelNumber.Third);
        readySession.DoorAndMaze.AddDoor(new DoorId(Guid.NewGuid()), new GridPosition(9, 9));
        new UpdateThirdLevelDoorStateUseCase(readySession).Execute(
            new UpdateThirdLevelDoorStateCommand(6000));

        Assert.Equal(DoorPhase.Open, Assert.Single(readySession.DoorAndMaze.Doors).Phase);
    }

    private SnakeGameSession CreateThirdLevelSessionWithDoor(GridPosition position)
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third);
        session.DoorAndMaze.AddDoor(new DoorId(Guid.NewGuid()), position);
        return session;
    }
}
