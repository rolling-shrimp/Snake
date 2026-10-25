using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class ResetGameForReplayAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-RESET-GAME-FOR-REPLAY-001")]
    public void SCN_RESET_GAME_FOR_REPLAY_001_ResetsFailedGameDirectlyToFirstLevelPlaying()
    {
        SnakeGameSession session = CreateGameOverSession();
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        RestartGameAcceptedReactor reactor = new RestartGameAcceptedReactor(
            new ResetGameForReplayUseCase(session, outputPort));

        ResetGameForReplayResult result = reactor.React(new RestartGameAcceptedDomainEvent());

        Assert.True(result.IsSuccess);
        Assert.Equal(LevelNumber.First, session.Level.Number);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
        Assert.Equal(3, session.Snake.Length);
        Assert.Equal(15, session.Level.GetProgressPercent(session.Snake.Length));
        Assert.Single(session.Food.ActiveItems);
        Assert.Empty(session.GameResult.LevelResults);
        Assert.Equal(new List<string>
        {
            "hide-level-1-ready",
            "show-Strawberry",
            "start-movement"
        }, outputPort.Operations);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-RESET-GAME-FOR-REPLAY-002")]
    public void SCN_RESET_GAME_FOR_REPLAY_002_ClearsCompletedGameAndStartsSameFirstLevelState()
    {
        SnakeGameSession session = CreateCompletedSession();
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        RestartGameAcceptedReactor reactor = new RestartGameAcceptedReactor(
            new ResetGameForReplayUseCase(session, outputPort));

        ResetGameForReplayResult result = reactor.React(new RestartGameAcceptedDomainEvent());

        Assert.True(result.IsSuccess);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
        Assert.Equal(LevelNumber.First, session.Level.Number);
        Assert.Empty(session.GameResult.LevelResults);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-RESET-GAME-FOR-REPLAY-003")]
    public void SCN_RESET_GAME_FOR_REPLAY_003_IgnoresUnknownDuplicateAndNonTerminalEvents()
    {
        SnakeGameSession session = CreateGameOverSession();
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        RestartGameAcceptedReactor reactor = new RestartGameAcceptedReactor(
            new ResetGameForReplayUseCase(session, outputPort));

        reactor.React(new UnknownDomainEvent());
        RestartGameAcceptedDomainEvent domainEvent = new RestartGameAcceptedDomainEvent();
        reactor.React(domainEvent);
        ResetGameForReplayResult duplicateResult = reactor.React(domainEvent);

        Assert.True(duplicateResult.IsSuccess);
        Assert.Equal(3, outputPort.Operations.Count);

        SnakeGameSession playingSession = new GameTestSessionFactory().CreatePlaying(LevelNumber.First);
        StubAdvancedLevelPlayOutputPort playingOutput = new StubAdvancedLevelPlayOutputPort();
        RestartGameAcceptedReactor playingReactor = new RestartGameAcceptedReactor(
            new ResetGameForReplayUseCase(playingSession, playingOutput));

        playingReactor.React(new RestartGameAcceptedDomainEvent());

        Assert.Empty(playingOutput.Operations);
        Assert.Equal(GamePhase.Playing, playingSession.Game.Phase);
    }

    private SnakeGameSession CreateGameOverSession()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second, 4);
        session.GameResult.Record(
            new LevelResultId(Guid.NewGuid()),
            LevelNumber.Second,
            Snake.Entities.LevelOutcome.Failed,
            4,
            Snake.Entities.DeathCause.Bomb);
        session.Game.EndGame();
        return session;
    }

    private SnakeGameSession CreateCompletedSession()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third, 51);
        session.GameResult.Record(
            new LevelResultId(Guid.NewGuid()),
            LevelNumber.Third,
            Snake.Entities.LevelOutcome.Passed,
            51,
            null);
        session.Game.Complete();
        return session;
    }
}
