using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class PrepareNextLevelAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-PREPARE-NEXT-LEVEL-001")]
    public void SCN_PREPARE_NEXT_LEVEL_001_PreparesCleanSecondLevelReadyAndPreservesResult()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First, 20);
        session.GameResult.Record(
            new LevelResultId(Guid.NewGuid()),
            LevelNumber.First,
            Snake.Entities.LevelOutcome.Passed,
            20,
            null);
        session.Food.SpawnOrdinary(new FoodItemId(Guid.NewGuid()), new GridPosition(9, 9));
        PrepareNextLevelUseCase useCase = new PrepareNextLevelUseCase(session);

        PrepareNextLevelResult result = useCase.Execute(new PrepareNextLevelCommand(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(LevelNumber.Second, session.Level.Number);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
        Assert.Equal(3, session.Snake.Length);
        Assert.Equal(Snake.Entities.Direction.Right, session.Snake.CurrentDirection);
        Assert.Equal(15, session.Level.GetProgressPercent(session.Snake.Length));
        Assert.Equal(0, session.Shield.RemainingCharges);
        Assert.Empty(session.Food.ActiveItems);
        Assert.Single(session.GameResult.LevelResults);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-PREPARE-NEXT-LEVEL-002")]
    public void SCN_PREPARE_NEXT_LEVEL_002_PreparesThirdLevelMazeReadyWithoutStartingCycle()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second, 20);
        session.GameResult.Record(
            new LevelResultId(Guid.NewGuid()),
            LevelNumber.Second,
            Snake.Entities.LevelOutcome.Passed,
            20,
            null);
        PrepareNextLevelUseCase useCase = new PrepareNextLevelUseCase(session);

        PrepareNextLevelResult result = useCase.Execute(new PrepareNextLevelCommand(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(LevelNumber.Third, session.Level.Number);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
        Assert.Equal(3, session.Snake.Length);
        Assert.Equal(6, session.Level.GetProgressPercent(session.Snake.Length));
        Assert.Equal(0, session.Shield.RemainingCharges);
        Assert.NotEmpty(session.DoorAndMaze.WallCells);
        Assert.NotEmpty(session.DoorAndMaze.Doors);
        Assert.All(session.DoorAndMaze.Doors, door => Assert.Equal(DoorPhase.Open, door.Phase));
    }

    [Fact]
    [Trait("ScenarioId", "SCN-PREPARE-NEXT-LEVEL-003")]
    public void SCN_PREPARE_NEXT_LEVEL_003_IgnoresThirdLevelAndFailedResult()
    {
        SnakeGameSession thirdSession = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third, 50);
        thirdSession.GameResult.Record(
            new LevelResultId(Guid.NewGuid()),
            LevelNumber.Third,
            Snake.Entities.LevelOutcome.Passed,
            50,
            null);
        PrepareNextLevelUseCase thirdUseCase = new PrepareNextLevelUseCase(thirdSession);

        PrepareNextLevelResult thirdResult = thirdUseCase.Execute(new PrepareNextLevelCommand(3));

        Assert.True(thirdResult.IsSuccess);
        Assert.Equal(LevelNumber.Third, thirdSession.Level.Number);
        Assert.Equal(GamePhase.Playing, thirdSession.Game.Phase);

        SnakeGameSession failedSession = new GameTestSessionFactory().CreatePlaying(LevelNumber.First);
        failedSession.GameResult.Record(
            new LevelResultId(Guid.NewGuid()),
            LevelNumber.First,
            Snake.Entities.LevelOutcome.Failed,
            3,
            Snake.Entities.DeathCause.SelfCollision);
        PrepareNextLevelUseCase failedUseCase = new PrepareNextLevelUseCase(failedSession);

        failedUseCase.Execute(new PrepareNextLevelCommand(1));

        Assert.Equal(LevelNumber.First, failedSession.Level.Number);
        Assert.Equal(GamePhase.Playing, failedSession.Game.Phase);
    }
}
