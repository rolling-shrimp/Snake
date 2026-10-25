using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class HandleShieldContactAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-SHIELD-CONTACT-001")]
    public void SCN_HANDLE_SHIELD_CONTACT_001_CollectsOneChargeWithoutResettingSpawnCount()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second);
        ShieldItemId shieldItemId = new ShieldItemId(Guid.NewGuid());
        session.Shield.Spawn(shieldItemId, session.Snake.BodyCells[0]);
        HandleShieldContactUseCase useCase = new HandleShieldContactUseCase(session);

        HandleShieldContactResult result = useCase.Execute(
            new HandleShieldContactCommand(shieldItemId.Value.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Null(session.Shield.ActiveItem);
        Assert.Equal(1, session.Shield.RemainingCharges);
        Assert.Equal(1, session.Shield.SpawnedCount);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-SHIELD-CONTACT-002")]
    public void SCN_HANDLE_SHIELD_CONTACT_002_IgnoresConsumedOrWrongLevelItem()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second);
        ShieldItemId shieldItemId = new ShieldItemId(Guid.NewGuid());
        session.Shield.Spawn(shieldItemId, session.Snake.BodyCells[0]);
        HandleShieldContactUseCase useCase = new HandleShieldContactUseCase(session);
        useCase.Execute(new HandleShieldContactCommand(shieldItemId.Value.ToString()));

        HandleShieldContactResult consumedResult = useCase.Execute(
            new HandleShieldContactCommand(shieldItemId.Value.ToString()));

        Assert.True(consumedResult.IsSuccess);
        Assert.Equal(1, session.Shield.RemainingCharges);

        SnakeGameSession firstLevelSession = new GameTestSessionFactory().CreatePlaying(LevelNumber.First);
        ShieldItemId firstLevelItemId = new ShieldItemId(Guid.NewGuid());
        firstLevelSession.Shield.Spawn(firstLevelItemId, firstLevelSession.Snake.BodyCells[0]);
        HandleShieldContactUseCase firstLevelUseCase = new HandleShieldContactUseCase(firstLevelSession);

        firstLevelUseCase.Execute(new HandleShieldContactCommand(firstLevelItemId.Value.ToString()));

        Assert.Equal(0, firstLevelSession.Shield.RemainingCharges);
        Assert.NotNull(firstLevelSession.Shield.ActiveItem);
    }
}
