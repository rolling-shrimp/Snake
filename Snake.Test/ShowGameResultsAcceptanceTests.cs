using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class ShowGameResultsAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-LEVEL-PASSED-RESULT-001")]
    public void SCN_SHOW_LEVEL_PASSED_RESULT_001_ReturnsActualPassedLengthWhileNextLevelIsReady()
    {
        SnakeGameSession session = CreateEndedSession(
            LevelNumber.First,
            21,
            Snake.UseCases.LevelOutcome.Passed,
            null);
        ShowLevelPassedResultUseCase useCase = new ShowLevelPassedResultUseCase(session);

        ShowLevelPassedResultResult result = useCase.Execute(new ShowLevelPassedResultQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data.CompletedLevel);
        Assert.Equal(21, result.Data.FinalLength);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.BannerText));
        Assert.True(result.Data.IsScoreCardVisible);
        Assert.Equal(LevelNumber.Second, session.Level.Number);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-LEVEL-PASSED-RESULT-002")]
    public void SCN_SHOW_LEVEL_PASSED_RESULT_002_HidesOldResultAfterNextLevelStarts()
    {
        SnakeGameSession session = CreateEndedSession(
            LevelNumber.First,
            20,
            Snake.UseCases.LevelOutcome.Passed,
            null);
        session.Game.ActivateLevel();
        ShowLevelPassedResultUseCase useCase = new ShowLevelPassedResultUseCase(session);

        ShowLevelPassedResultResult result = useCase.Execute(new ShowLevelPassedResultQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("RESULT_NOT_AVAILABLE", result.FailureCode);
        Assert.Equal("目前沒有可顯示的關卡通過結果。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-GAME-OVER-RESULT-001")]
    public void SCN_SHOW_GAME_OVER_RESULT_001_ReturnsFailureDetailsAndClampedProgress()
    {
        SnakeGameSession session = CreateEndedSession(
            LevelNumber.Second,
            2,
            Snake.UseCases.LevelOutcome.Failed,
            Snake.UseCases.DeathCause.FatalFire);
        ShowGameOverResultUseCase useCase = new ShowGameOverResultUseCase(session);

        ShowGameOverResultResult result = useCase.Execute(new ShowGameOverResultQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.FailedLevel);
        Assert.Equal(0, result.Data.FinalLength);
        Assert.Equal(Snake.UseCases.DeathCause.FatalFire, result.Data.DeathCause);
        Assert.Equal(0, result.Data.ProgressPercent);
        Assert.True(result.Data.IsReplayButtonVisible);
        Assert.Equal(GamePhase.GameOver, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-GAME-OVER-RESULT-002")]
    public void SCN_SHOW_GAME_OVER_RESULT_002_RejectsUnavailableStateWithoutData()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.First);
        ShowGameOverResultUseCase useCase = new ShowGameOverResultUseCase(session);

        ShowGameOverResultResult result = useCase.Execute(new ShowGameOverResultQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("RESULT_NOT_AVAILABLE", result.FailureCode);
        Assert.Equal("目前沒有可顯示的失敗結果。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-GAME-COMPLETED-RESULT-001")]
    public void SCN_SHOW_GAME_COMPLETED_RESULT_001_ReturnsActualThirdLevelLengthAndFullProgress()
    {
        SnakeGameSession session = CreateEndedSession(
            LevelNumber.Third,
            51,
            Snake.UseCases.LevelOutcome.Passed,
            null);
        ShowGameCompletedResultUseCase useCase = new ShowGameCompletedResultUseCase(session);

        ShowGameCompletedResultResult result = useCase.Execute(new ShowGameCompletedResultQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(51, result.Data.FinalLength);
        Assert.Equal(100, result.Data.ProgressPercent);
        Assert.False(string.IsNullOrWhiteSpace(result.Data.SuccessMessage));
        Assert.True(result.Data.IsReplayButtonVisible);
        Assert.Equal(GamePhase.Completed, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-GAME-COMPLETED-RESULT-002")]
    public void SCN_SHOW_GAME_COMPLETED_RESULT_002_RejectsUnavailableStateWithoutData()
    {
        SnakeGameSession session = CreateEndedSession(
            LevelNumber.Third,
            49,
            Snake.UseCases.LevelOutcome.Failed,
            Snake.UseCases.DeathCause.Bomb);
        ShowGameCompletedResultUseCase useCase = new ShowGameCompletedResultUseCase(session);

        ShowGameCompletedResultResult result = useCase.Execute(new ShowGameCompletedResultQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("RESULT_NOT_AVAILABLE", result.FailureCode);
        Assert.Equal("目前沒有可顯示的整輪完成結果。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(GamePhase.GameOver, session.Game.Phase);
    }

    private SnakeGameSession CreateEndedSession(
        LevelNumber levelNumber,
        int snakeLength,
        Snake.UseCases.LevelOutcome outcome,
        Snake.UseCases.DeathCause? deathCause)
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(levelNumber, snakeLength);
        EndCurrentLevelPlayUseCase endUseCase = new EndCurrentLevelPlayUseCase(
            session,
            new PrepareNextLevelUseCase(session));
        endUseCase.Execute(new EndCurrentLevelPlayCommand(outcome, deathCause));
        return session;
    }
}
