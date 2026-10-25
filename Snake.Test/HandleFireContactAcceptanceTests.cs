using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class HandleFireContactAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-FIRE-CONTACT-001")]
    public void SCN_HANDLE_FIRE_CONTACT_001_RemovesFireAndCommitsNonFatalReduction()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second, 5);
        FireItemId fireItemId = new FireItemId(Guid.NewGuid());
        session.Fire.Spawn(fireItemId, session.Snake.BodyCells[0]);
        int shieldChargesBefore = session.Shield.RemainingCharges;
        HandleFireContactUseCase useCase = new HandleFireContactUseCase(session);

        HandleFireContactResult result = useCase.Execute(
            new HandleFireContactCommand(fireItemId.Value.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Null(session.Fire.ActiveItem);
        Assert.Equal(3, session.Snake.Length);
        Assert.Equal(shieldChargesBefore, session.Shield.RemainingCharges);
        SnakeLengthChangedDomainEvent lengthChanged = Assert.Single(
            session.PublishedFacts.OfType<SnakeLengthChangedDomainEvent>());
        Assert.Equal(3, lengthChanged.CurrentLength);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-FIRE-CONTACT-002")]
    public void SCN_HANDLE_FIRE_CONTACT_002_PublishesFatalFireWithoutCommittingZeroLength()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second, 2);
        FireItemId fireItemId = new FireItemId(Guid.NewGuid());
        session.Fire.Spawn(fireItemId, session.Snake.BodyCells[0]);
        HandleFireContactUseCase useCase = new HandleFireContactUseCase(session);

        HandleFireContactResult result = useCase.Execute(
            new HandleFireContactCommand(fireItemId.Value.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Null(session.Fire.ActiveItem);
        Assert.Equal(2, session.Snake.Length);
        FatalFireDomainEvent fatalFire = Assert.Single(session.PublishedFacts.OfType<FatalFireDomainEvent>());
        Assert.Equal(2, fatalFire.PreviousLength);
        Assert.Equal(0, fatalFire.EffectiveLength);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-FIRE-CONTACT-003")]
    public void SCN_HANDLE_FIRE_CONTACT_003_IgnoresFirstLevelAndInactiveFire()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First, 5);
        FireItemId fireItemId = new FireItemId(Guid.NewGuid());
        session.Fire.Spawn(fireItemId, session.Snake.BodyCells[0]);
        HandleFireContactUseCase useCase = new HandleFireContactUseCase(session);

        HandleFireContactResult firstLevelResult = useCase.Execute(
            new HandleFireContactCommand(fireItemId.Value.ToString()));
        HandleFireContactResult inactiveResult = useCase.Execute(
            new HandleFireContactCommand(Guid.NewGuid().ToString()));

        Assert.True(firstLevelResult.IsSuccess);
        Assert.True(inactiveResult.IsSuccess);
        Assert.Equal(5, session.Snake.Length);
        Assert.NotNull(session.Fire.ActiveItem);
        Assert.Empty(session.PublishedFacts);
    }
}
