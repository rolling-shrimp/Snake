using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class HandleWallCollisionAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-WALL-COLLISION-001")]
    public void SCN_HANDLE_WALL_COLLISION_001_PublishesOnceInThirdLevelWithoutMovingSnake()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third);
        HandleWallCollisionUseCase useCase = new HandleWallCollisionUseCase(session);
        List<GridPosition> bodyBefore = session.Snake.BodyCells;

        HandleWallCollisionResult firstResult = useCase.Execute(
            new HandleWallCollisionCommand(new GridPositionDTO(0, 5)));
        HandleWallCollisionResult duplicateResult = useCase.Execute(
            new HandleWallCollisionCommand(new GridPositionDTO(0, 5)));

        Assert.True(firstResult.IsSuccess);
        Assert.True(duplicateResult.IsSuccess);
        Assert.IsType<WallCollisionDomainEvent>(Assert.Single(session.PublishedFacts));
        Assert.Equal(bodyBefore, session.Snake.BodyCells);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-WALL-COLLISION-002")]
    public void SCN_HANDLE_WALL_COLLISION_002_IgnoresWrapLevels()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second);
        HandleWallCollisionUseCase useCase = new HandleWallCollisionUseCase(session);

        HandleWallCollisionResult result = useCase.Execute(
            new HandleWallCollisionCommand(new GridPositionDTO(0, 5)));

        Assert.True(result.IsSuccess);
        Assert.Empty(session.PublishedFacts);
    }
}
