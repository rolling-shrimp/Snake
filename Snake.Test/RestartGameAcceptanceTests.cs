using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class RestartGameAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-RESTART-GAME-001")]
    public void SCN_RESTART_GAME_001_AcceptsTerminalRestartOnceAndHandsOffReset()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third, 50);
        session.Game.Complete();
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        RestartGameAcceptedReactor reactor = new RestartGameAcceptedReactor(
            new ResetGameForReplayUseCase(session, outputPort));
        RestartGameUseCase useCase = new RestartGameUseCase(session, reactor);

        RestartGameResult firstResult = useCase.Execute(new RestartGameCommand());
        RestartGameResult duplicateResult = useCase.Execute(new RestartGameCommand());

        Assert.True(firstResult.IsSuccess);
        Assert.True(duplicateResult.IsSuccess);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
        Assert.Equal(3, outputPort.Operations.Count);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-RESTART-GAME-002")]
    public void SCN_RESTART_GAME_002_IgnoresNonTerminalGame()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First);
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        RestartGameUseCase useCase = new RestartGameUseCase(
            session,
            new RestartGameAcceptedReactor(new ResetGameForReplayUseCase(session, outputPort)));

        RestartGameResult result = useCase.Execute(new RestartGameCommand());

        Assert.True(result.IsSuccess);
        Assert.Empty(outputPort.Operations);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
    }
}
