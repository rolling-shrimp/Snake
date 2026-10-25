using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class HandleSelfCollisionAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-SELF-COLLISION-001")]
    public void SCN_HANDLE_SELF_COLLISION_001_PublishesOnceWithoutMovingSnake()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First);
        HandleSelfCollisionUseCase useCase = new HandleSelfCollisionUseCase(session);
        List<GridPosition> bodyBefore = session.Snake.BodyCells;
        GridPosition collisionPosition = bodyBefore[1];

        HandleSelfCollisionResult firstResult = useCase.Execute(new HandleSelfCollisionCommand(
            new GridPositionDTO(collisionPosition.X, collisionPosition.Y)));
        HandleSelfCollisionResult duplicateResult = useCase.Execute(new HandleSelfCollisionCommand(
            new GridPositionDTO(collisionPosition.X, collisionPosition.Y)));

        Assert.True(firstResult.IsSuccess);
        Assert.True(duplicateResult.IsSuccess);
        Assert.IsType<SelfCollisionDomainEvent>(Assert.Single(session.PublishedFacts));
        Assert.Equal(bodyBefore, session.Snake.BodyCells);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-SELF-COLLISION-002")]
    public void SCN_HANDLE_SELF_COLLISION_002_IgnoresNonOverlappingPosition()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First);
        HandleSelfCollisionUseCase useCase = new HandleSelfCollisionUseCase(session);

        HandleSelfCollisionResult result = useCase.Execute(
            new HandleSelfCollisionCommand(new GridPositionDTO(9, 9)));

        Assert.True(result.IsSuccess);
        Assert.Empty(session.PublishedFacts);
    }
}
