using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class ActivateSecondLevelPlayAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-ACTIVATE-SECOND-LEVEL-PLAY-001")]
    public void SCN_ACTIVATE_SECOND_LEVEL_PLAY_001_SpawnsFourObjectsThenStartsMovement()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        StartSecondLevelAcceptedReactor reactor = new StartSecondLevelAcceptedReactor(
            new ActivateSecondLevelPlayUseCase(session, outputPort));

        ActivateSecondLevelPlayResult result = reactor.React(new StartSecondLevelAcceptedDomainEvent());

        Assert.True(result.IsSuccess);
        Assert.Equal(new List<string>
        {
            "hide-level-2-ready",
            "show-Strawberry",
            "show-SpecialFood",
            "show-Shield",
            "show-Fire",
            "start-movement"
        }, outputPort.Operations);
        Assert.Equal(2, session.Food.ActiveItems.Count);
        Assert.NotNull(session.Shield.ActiveItem);
        Assert.NotNull(session.Fire.ActiveItem);
        Assert.Equal(0, session.Shield.RemainingCharges);
        Assert.Equal(3, session.Snake.Length);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-ACTIVATE-SECOND-LEVEL-PLAY-002")]
    public void SCN_ACTIVATE_SECOND_LEVEL_PLAY_002_IgnoresUnknownEvent()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        StartSecondLevelAcceptedReactor reactor = new StartSecondLevelAcceptedReactor(
            new ActivateSecondLevelPlayUseCase(session, outputPort));

        ActivateSecondLevelPlayResult result = reactor.React(new UnknownDomainEvent());

        Assert.True(result.IsSuccess);
        Assert.Empty(outputPort.Operations);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-ACTIVATE-SECOND-LEVEL-PLAY-003")]
    public void SCN_ACTIVATE_SECOND_LEVEL_PLAY_003_IgnoresDuplicateEvent()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        StartSecondLevelAcceptedReactor reactor = new StartSecondLevelAcceptedReactor(
            new ActivateSecondLevelPlayUseCase(session, outputPort));
        StartSecondLevelAcceptedDomainEvent domainEvent = new StartSecondLevelAcceptedDomainEvent();

        reactor.React(domainEvent);
        ActivateSecondLevelPlayResult duplicateResult = reactor.React(domainEvent);

        Assert.True(duplicateResult.IsSuccess);
        Assert.Equal(6, outputPort.Operations.Count);
        Assert.Equal(2, session.Food.ActiveItems.Count);
    }
}
