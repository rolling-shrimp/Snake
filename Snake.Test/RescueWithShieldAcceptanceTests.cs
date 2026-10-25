using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class RescueWithShieldAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-RESCUE-WITH-SHIELD-001")]
    public void SCN_RESCUE_WITH_SHIELD_001_ConsumesChargeAndRestoresFullLengthSafely()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second, 5);
        GiveShieldCharge(session);
        session.Food.SpawnOrdinary(new FoodItemId(Guid.NewGuid()), new GridPosition(0, 0));
        RescueWithShieldUseCase useCase = CreateUseCase(session);

        RescueWithShieldResult result = useCase.Execute(
            new RescueWithShieldCommand(Snake.UseCases.DeathCause.SelfCollision, 5));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, session.Shield.RemainingCharges);
        Assert.Equal(5, session.Snake.Length);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
        Assert.DoesNotContain(
            session.Snake.BodyCells,
            cell => session.GetObjectOccupiedCells().Contains(cell));
    }

    [Fact]
    [Trait("ScenarioId", "SCN-RESCUE-WITH-SHIELD-002")]
    public void SCN_RESCUE_WITH_SHIELD_002_PreservesPreHitLengthForFatalFire()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second, 2);
        GiveShieldCharge(session);
        RescueWithShieldUseCase useCase = CreateUseCase(session);

        RescueWithShieldResult result = useCase.Execute(
            new RescueWithShieldCommand(Snake.UseCases.DeathCause.FatalFire, 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, session.Snake.Length);
        Assert.Equal(0, session.Shield.RemainingCharges);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-RESCUE-WITH-SHIELD-003")]
    public void SCN_RESCUE_WITH_SHIELD_003_HandsUnrescuedDeathToEndCurrentLevelOnce()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second, 4);
        RescueWithShieldUseCase useCase = CreateUseCase(session);

        RescueWithShieldResult result = useCase.Execute(
            new RescueWithShieldCommand(Snake.UseCases.DeathCause.Bomb, 4));

        Assert.True(result.IsSuccess);
        Assert.Equal(GamePhase.GameOver, session.Game.Phase);
        LevelResult levelResult = Assert.Single(session.GameResult.LevelResults);
        Assert.Equal(Snake.Entities.DeathCause.Bomb, levelResult.DeathCause);
    }

    private RescueWithShieldUseCase CreateUseCase(SnakeGameSession session)
    {
        PrepareNextLevelUseCase prepareUseCase = new PrepareNextLevelUseCase(session);
        EndCurrentLevelPlayUseCase endUseCase = new EndCurrentLevelPlayUseCase(session, prepareUseCase);
        return new RescueWithShieldUseCase(session, endUseCase);
    }

    private void GiveShieldCharge(SnakeGameSession session)
    {
        ShieldItemId shieldItemId = new ShieldItemId(Guid.NewGuid());
        session.Shield.Spawn(shieldItemId, new GridPosition(9, 9));
        session.Shield.Collect(shieldItemId);
    }
}
