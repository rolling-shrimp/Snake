using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class ActivateThirdLevelPlayAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-ACTIVATE-THIRD-LEVEL-PLAY-001")]
    public void SCN_ACTIVATE_THIRD_LEVEL_PLAY_001_SpawnsFiveObjectsThenStartsMovementAndDoorCycle()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Third);
        session.DoorAndMaze.PrepareThirdLevel(new DoorId(Guid.NewGuid()));
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        StartThirdLevelAcceptedReactor reactor = new StartThirdLevelAcceptedReactor(
            new ActivateThirdLevelPlayUseCase(session, outputPort));

        ActivateThirdLevelPlayResult result = reactor.React(new StartThirdLevelAcceptedDomainEvent());

        Assert.True(result.IsSuccess);
        Assert.Equal(new List<string>
        {
            "hide-level-3-ready",
            "show-Strawberry",
            "show-SpecialFood",
            "show-Shield",
            "show-Fire",
            "show-Bomb",
            "start-movement",
            "start-door-cycle"
        }, outputPort.Operations);
        Assert.Equal(2, session.Food.ActiveItems.Count);
        Assert.NotNull(session.Shield.ActiveItem);
        Assert.NotNull(session.Fire.ActiveItem);
        Assert.NotNull(session.Bomb.ActiveItem);
        Assert.Equal(3, session.Snake.Length);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-ACTIVATE-THIRD-LEVEL-PLAY-002")]
    public void SCN_ACTIVATE_THIRD_LEVEL_PLAY_002_IgnoresUnknownEvent()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Third);
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        StartThirdLevelAcceptedReactor reactor = new StartThirdLevelAcceptedReactor(
            new ActivateThirdLevelPlayUseCase(session, outputPort));

        ActivateThirdLevelPlayResult result = reactor.React(new UnknownDomainEvent());

        Assert.True(result.IsSuccess);
        Assert.Empty(outputPort.Operations);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-ACTIVATE-THIRD-LEVEL-PLAY-003")]
    public void SCN_ACTIVATE_THIRD_LEVEL_PLAY_003_IgnoresDuplicateEvent()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Third);
        session.DoorAndMaze.PrepareThirdLevel(new DoorId(Guid.NewGuid()));
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        StartThirdLevelAcceptedReactor reactor = new StartThirdLevelAcceptedReactor(
            new ActivateThirdLevelPlayUseCase(session, outputPort));
        StartThirdLevelAcceptedDomainEvent domainEvent = new StartThirdLevelAcceptedDomainEvent();

        reactor.React(domainEvent);
        ActivateThirdLevelPlayResult duplicateResult = reactor.React(domainEvent);

        Assert.True(duplicateResult.IsSuccess);
        Assert.Equal(8, outputPort.Operations.Count);
        Assert.NotNull(session.Bomb.ActiveItem);
    }
}
