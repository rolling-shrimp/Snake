using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class AdvanceSnakeStepAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-ADVANCE-SNAKE-STEP-001")]
    public void SCN_ADVANCE_SNAKE_STEP_001_WrapsAcrossBoundaryAndMovesExactlyOneCell()
    {
        SnakeGameSession session = CreatePlayingSession(LevelNumber.First);
        AdvanceSnakeStepUseCase useCase = new AdvanceSnakeStepUseCase(session);

        AdvanceSnakeStepResult result = useCase.Execute(new AdvanceSnakeStepCommand(250));

        Assert.True(result.IsSuccess);
        Assert.Null(result.FailureCode);
        Assert.Null(result.FailureMessage);
        Assert.Equal(new GridPosition(0, 1), session.Snake.BodyCells[0]);
        Assert.Equal(new GridPosition(9, 1), session.Snake.BodyCells[1]);
        Assert.Equal(new GridPosition(8, 1), session.Snake.BodyCells[2]);
        Assert.Null(session.LastMovementFact);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-ADVANCE-SNAKE-STEP-002")]
    public void SCN_ADVANCE_SNAKE_STEP_002_BlocksThirdLevelBoundaryAndProducesWallCollision()
    {
        SnakeGameSession session = CreatePlayingSession(LevelNumber.Third);
        AdvanceSnakeStepUseCase useCase = new AdvanceSnakeStepUseCase(session);
        List<GridPosition> bodyBefore = session.Snake.BodyCells;

        AdvanceSnakeStepResult result = useCase.Execute(new AdvanceSnakeStepCommand(250));

        Assert.True(result.IsSuccess);
        Assert.Equal(bodyBefore, session.Snake.BodyCells);
        WallCollisionDomainEvent collision = Assert.IsType<WallCollisionDomainEvent>(session.LastMovementFact);
        Assert.Equal(10, collision.AttemptedX);
        Assert.Equal(1, collision.AttemptedY);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-ADVANCE-SNAKE-STEP-003")]
    public void SCN_ADVANCE_SNAKE_STEP_003_IgnoresEarlyIntervalAndNonPlayingGame()
    {
        SnakeGameSession playingSession = CreatePlayingSession(LevelNumber.First);
        AdvanceSnakeStepUseCase playingUseCase = new AdvanceSnakeStepUseCase(playingSession);
        List<GridPosition> playingBodyBefore = playingSession.Snake.BodyCells;

        AdvanceSnakeStepResult earlyResult = playingUseCase.Execute(new AdvanceSnakeStepCommand(249));

        Assert.True(earlyResult.IsSuccess);
        Assert.Equal(playingBodyBefore, playingSession.Snake.BodyCells);

        SnakeGameSession readySession = CreateReadySession(LevelNumber.First);
        AdvanceSnakeStepUseCase readyUseCase = new AdvanceSnakeStepUseCase(readySession);
        List<GridPosition> readyBodyBefore = readySession.Snake.BodyCells;

        AdvanceSnakeStepResult readyResult = readyUseCase.Execute(new AdvanceSnakeStepCommand(250));

        Assert.True(readyResult.IsSuccess);
        Assert.Equal(readyBodyBefore, readySession.Snake.BodyCells);
        Assert.Null(readySession.LastMovementFact);
    }

    [Fact]
    public void Execute_DoesNotCommitSelfCollisionMovement()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First);
        session.Snake.Reset(new List<GridPosition>
        {
            new GridPosition(2, 2),
            new GridPosition(3, 2),
            new GridPosition(3, 3)
        });
        List<GridPosition> bodyBefore = session.Snake.BodyCells;
        AdvanceSnakeStepUseCase useCase = new AdvanceSnakeStepUseCase(session);

        useCase.Execute(new AdvanceSnakeStepCommand(250));

        Assert.Equal(bodyBefore, session.Snake.BodyCells);
        SelfCollisionDomainEvent collision = Assert.IsType<SelfCollisionDomainEvent>(session.LastMovementFact);
        Assert.Equal(new GridPosition(3, 2), collision.CollisionPosition);
    }

    [Fact]
    public void Execute_DoesNotCommitThirdLevelInnerWallMovement()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third);
        session.DoorAndMaze.PrepareThirdLevel(new DoorId(Guid.NewGuid()));
        session.Snake.Reset(new List<GridPosition>
        {
            new GridPosition(4, 2),
            new GridPosition(3, 2),
            new GridPosition(2, 2)
        });
        List<GridPosition> bodyBefore = session.Snake.BodyCells;
        AdvanceSnakeStepUseCase useCase = new AdvanceSnakeStepUseCase(session);

        useCase.Execute(new AdvanceSnakeStepCommand(250));

        Assert.Equal(bodyBefore, session.Snake.BodyCells);
        WallCollisionDomainEvent collision = Assert.IsType<WallCollisionDomainEvent>(session.LastMovementFact);
        Assert.Equal(5, collision.AttemptedX);
        Assert.Equal(2, collision.AttemptedY);
    }

    [Fact]
    public void Execute_DoesNotCommitClosedDoorMovement()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third);
        DoorId doorId = new DoorId(Guid.NewGuid());
        session.DoorAndMaze.PrepareThirdLevel(doorId);
        Door door = session.DoorAndMaze.FindDoor(doorId)
            ?? throw new InvalidOperationException("測試門不存在。");
        door.ChangePhase(DoorPhase.Closed);
        session.Snake.Reset(new List<GridPosition>
        {
            new GridPosition(4, 5),
            new GridPosition(3, 5),
            new GridPosition(2, 5)
        });
        List<GridPosition> bodyBefore = session.Snake.BodyCells;
        AdvanceSnakeStepUseCase useCase = new AdvanceSnakeStepUseCase(session);

        useCase.Execute(new AdvanceSnakeStepCommand(250));

        Assert.Equal(bodyBefore, session.Snake.BodyCells);
        ClosedDoorCollisionDomainEvent collision = Assert.IsType<ClosedDoorCollisionDomainEvent>(session.LastMovementFact);
        Assert.Equal(doorId, collision.DoorId);
    }

    private SnakeGameSession CreatePlayingSession(LevelNumber levelNumber)
    {
        SnakeGameSession session = CreateReadySession(levelNumber);
        session.Game.ActivateFirstLevel();
        return session;
    }

    private SnakeGameSession CreateReadySession(LevelNumber levelNumber)
    {
        Board board = new Board(new BoardId(Guid.NewGuid()));
        Snake.Entities.Snake snake = new Snake.Entities.Snake(
            new SnakeId(Guid.NewGuid()),
            new List<GridPosition>
            {
                new GridPosition(9, 1),
                new GridPosition(8, 1),
                new GridPosition(7, 1)
            });
        Level level = new Level(new LevelId(Guid.NewGuid()));
        while (level.Number != levelNumber)
        {
            level.Advance();
        }

        return new SnakeGameSession(
            new Game(new GameId(Guid.NewGuid())),
            board,
            snake,
            level,
            new Food(new FoodId(Guid.NewGuid())));
    }
}
