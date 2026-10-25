using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class HandleClosedDoorCollisionAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-CLOSED-DOOR-COLLISION-001")]
    public void SCN_HANDLE_CLOSED_DOOR_COLLISION_001_PublishesOnceWithoutMovingSnake()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third);
        DoorId doorId = new DoorId(Guid.NewGuid());
        session.DoorAndMaze.AddDoor(doorId, new GridPosition(9, 9));
        session.DoorAndMaze.Update(6000, session.Snake.BodyCells);
        HandleClosedDoorCollisionUseCase useCase = new HandleClosedDoorCollisionUseCase(session);
        List<GridPosition> bodyBefore = session.Snake.BodyCells;

        HandleClosedDoorCollisionResult firstResult = useCase.Execute(
            new HandleClosedDoorCollisionCommand(doorId.Value.ToString()));
        HandleClosedDoorCollisionResult duplicateResult = useCase.Execute(
            new HandleClosedDoorCollisionCommand(doorId.Value.ToString()));

        Assert.True(firstResult.IsSuccess);
        Assert.True(duplicateResult.IsSuccess);
        Assert.Single(session.PublishedFacts.OfType<ClosedDoorCollisionDomainEvent>());
        Assert.Equal(bodyBefore, session.Snake.BodyCells);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-CLOSED-DOOR-COLLISION-002")]
    public void SCN_HANDLE_CLOSED_DOOR_COLLISION_002_IgnoresPassableDoorPhases()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third);
        DoorId doorId = new DoorId(Guid.NewGuid());
        session.DoorAndMaze.AddDoor(doorId, new GridPosition(9, 9));
        HandleClosedDoorCollisionUseCase useCase = new HandleClosedDoorCollisionUseCase(session);

        HandleClosedDoorCollisionResult openResult = useCase.Execute(
            new HandleClosedDoorCollisionCommand(doorId.Value.ToString()));
        session.DoorAndMaze.Update(4000, session.Snake.BodyCells);
        HandleClosedDoorCollisionResult warningResult = useCase.Execute(
            new HandleClosedDoorCollisionCommand(doorId.Value.ToString()));

        Assert.True(openResult.IsSuccess);
        Assert.True(warningResult.IsSuccess);
        Assert.Empty(session.PublishedFacts);
    }
}
