using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class ReplenishTriggeredObjectAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-REPLENISH-TRIGGERED-OBJECT-001")]
    public void SCN_REPLENISH_TRIGGERED_OBJECT_001_CreatesNewItemAtLegalUnoccupiedCell()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second);
        ReplenishTriggeredObjectUseCase useCase = new ReplenishTriggeredObjectUseCase(session);
        string previousItemId = Guid.NewGuid().ToString();

        ReplenishTriggeredObjectResult result = useCase.Execute(
            new ReplenishTriggeredObjectCommand(GameObjectType.Strawberry, previousItemId));

        Assert.True(result.IsSuccess);
        FoodItem foodItem = Assert.Single(session.Food.ActiveItems);
        Assert.NotEqual(previousItemId, foodItem.FoodItemId.Value.ToString());
        Assert.DoesNotContain(foodItem.Position, session.Snake.BodyCells);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-REPLENISH-TRIGGERED-OBJECT-002")]
    public void SCN_REPLENISH_TRIGGERED_OBJECT_002_SpawnsOnlySecondShield()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second);
        ShieldItemId firstShieldId = new ShieldItemId(Guid.NewGuid());
        session.Shield.Spawn(firstShieldId, new GridPosition(9, 9));
        session.Shield.Collect(firstShieldId);
        ReplenishTriggeredObjectUseCase useCase = new ReplenishTriggeredObjectUseCase(session);

        useCase.Execute(new ReplenishTriggeredObjectCommand(
            GameObjectType.Shield,
            firstShieldId.Value.ToString()));

        ShieldItem secondShield = Assert.IsType<ShieldItem>(session.Shield.ActiveItem);
        Assert.Equal(2, session.Shield.SpawnedCount);
        session.Shield.Collect(secondShield.ShieldItemId);

        useCase.Execute(new ReplenishTriggeredObjectCommand(
            GameObjectType.Shield,
            secondShield.ShieldItemId.Value.ToString()));

        Assert.Null(session.Shield.ActiveItem);
        Assert.Equal(2, session.Shield.SpawnedCount);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-REPLENISH-TRIGGERED-OBJECT-003")]
    public void SCN_REPLENISH_TRIGGERED_OBJECT_003_RecordsPendingWhenBoardHasNoLegalCell()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third, 100);
        ReplenishTriggeredObjectUseCase useCase = new ReplenishTriggeredObjectUseCase(session);

        ReplenishTriggeredObjectResult result = useCase.Execute(
            new ReplenishTriggeredObjectCommand(GameObjectType.Strawberry, Guid.NewGuid().ToString()));

        Assert.True(result.IsSuccess);
        Assert.Empty(session.Food.ActiveItems);
        Assert.Contains(FoodType.Ordinary, session.Food.PendingTypes);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-REPLENISH-TRIGGERED-OBJECT-004")]
    public void SCN_REPLENISH_TRIGGERED_OBJECT_004_ClearsPendingAndDoesNotSpawnAfterLevelEnds()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second);
        session.Food.MarkPending(FoodType.Ordinary);
        session.Game.EndGame();
        ReplenishTriggeredObjectUseCase useCase = new ReplenishTriggeredObjectUseCase(session);

        ReplenishTriggeredObjectResult result = useCase.Execute(
            new ReplenishTriggeredObjectCommand(GameObjectType.Strawberry, Guid.NewGuid().ToString()));

        Assert.True(result.IsSuccess);
        Assert.Empty(session.Food.PendingTypes);
        Assert.Empty(session.Food.ActiveItems);
    }
}
