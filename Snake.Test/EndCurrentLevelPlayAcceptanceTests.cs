using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class EndCurrentLevelPlayAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-END-CURRENT-LEVEL-PLAY-001")]
    public void SCN_END_CURRENT_LEVEL_PLAY_001_RecordsPassedThenPreparesNextLevelOnce()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First, 20);
        PrepareNextLevelUseCase prepareUseCase = new PrepareNextLevelUseCase(session);
        EndCurrentLevelPlayUseCase useCase = new EndCurrentLevelPlayUseCase(session, prepareUseCase);

        EndCurrentLevelPlayResult result = useCase.Execute(
            new EndCurrentLevelPlayCommand(Snake.UseCases.LevelOutcome.Passed, null));

        Assert.True(result.IsSuccess);
        LevelResult levelResult = Assert.Single(session.GameResult.LevelResults);
        Assert.Equal(LevelNumber.First, levelResult.Level);
        Assert.Equal(Snake.Entities.LevelOutcome.Passed, levelResult.Outcome);
        Assert.Equal(20, levelResult.FinalLength);
        Assert.Equal(LevelNumber.Second, session.Level.Number);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-END-CURRENT-LEVEL-PLAY-002")]
    public void SCN_END_CURRENT_LEVEL_PLAY_002_CompletesThirdLevelWithActualLength()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third, 51);
        EndCurrentLevelPlayUseCase useCase = new EndCurrentLevelPlayUseCase(
            session,
            new PrepareNextLevelUseCase(session));

        EndCurrentLevelPlayResult result = useCase.Execute(
            new EndCurrentLevelPlayCommand(Snake.UseCases.LevelOutcome.Passed, null));

        Assert.True(result.IsSuccess);
        Assert.Equal(GamePhase.Completed, session.Game.Phase);
        Assert.Equal(51, Assert.Single(session.GameResult.LevelResults).FinalLength);
        Assert.Equal(51, session.Snake.Length);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-END-CURRENT-LEVEL-PLAY-003")]
    public void SCN_END_CURRENT_LEVEL_PLAY_003_RecordsFatalFireAtZeroAndEndsGame()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second, 2);
        EndCurrentLevelPlayUseCase useCase = new EndCurrentLevelPlayUseCase(
            session,
            new PrepareNextLevelUseCase(session));

        EndCurrentLevelPlayResult result = useCase.Execute(
            new EndCurrentLevelPlayCommand(
                Snake.UseCases.LevelOutcome.Failed,
                Snake.UseCases.DeathCause.FatalFire));

        Assert.True(result.IsSuccess);
        Assert.Equal(GamePhase.GameOver, session.Game.Phase);
        Assert.Equal(0, session.Snake.Length);
        LevelResult levelResult = Assert.Single(session.GameResult.LevelResults);
        Assert.Equal(0, levelResult.FinalLength);
        Assert.Equal(Snake.Entities.DeathCause.FatalFire, levelResult.DeathCause);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-END-CURRENT-LEVEL-PLAY-004")]
    public void SCN_END_CURRENT_LEVEL_PLAY_004_IgnoresDuplicateTerminalEvent()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third, 50);
        EndCurrentLevelPlayUseCase useCase = new EndCurrentLevelPlayUseCase(
            session,
            new PrepareNextLevelUseCase(session));

        EndCurrentLevelPlayResult firstResult = useCase.Execute(
            new EndCurrentLevelPlayCommand(Snake.UseCases.LevelOutcome.Passed, null));
        EndCurrentLevelPlayResult duplicateResult = useCase.Execute(
            new EndCurrentLevelPlayCommand(Snake.UseCases.LevelOutcome.Passed, null));

        Assert.True(firstResult.IsSuccess);
        Assert.True(duplicateResult.IsSuccess);
        Assert.Single(session.GameResult.LevelResults);
        Assert.Equal(GamePhase.Completed, session.Game.Phase);
    }
}
