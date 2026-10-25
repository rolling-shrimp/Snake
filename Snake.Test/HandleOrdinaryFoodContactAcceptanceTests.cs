using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class HandleOrdinaryFoodContactAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-ORDINARY-FOOD-CONTACT-001")]
    public void SCN_HANDLE_ORDINARY_FOOD_CONTACT_001_ConsumesFoodAndGrowsExactlyOne()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First);
        FoodItemId foodItemId = new FoodItemId(Guid.NewGuid());
        session.Food.SpawnOrdinary(foodItemId, session.Snake.BodyCells[0]);
        HandleOrdinaryFoodContactUseCase useCase = new HandleOrdinaryFoodContactUseCase(session);

        HandleOrdinaryFoodContactResult result = useCase.Execute(
            new HandleOrdinaryFoodContactCommand(foodItemId.Value.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Empty(session.Food.ActiveItems);
        Assert.Equal(4, session.Snake.Length);
        Assert.Single(session.PublishedFacts.OfType<FoodConsumedDomainEvent>());
        Assert.Single(session.PublishedFacts.OfType<SnakeLengthChangedDomainEvent>());
    }

    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-ORDINARY-FOOD-CONTACT-002")]
    public void SCN_HANDLE_ORDINARY_FOOD_CONTACT_002_IgnoresNonHeadContactAndConsumedItem()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First);
        FoodItemId foodItemId = new FoodItemId(Guid.NewGuid());
        session.Food.SpawnOrdinary(foodItemId, new GridPosition(9, 9));
        HandleOrdinaryFoodContactUseCase useCase = new HandleOrdinaryFoodContactUseCase(session);

        HandleOrdinaryFoodContactResult nonHeadResult = useCase.Execute(
            new HandleOrdinaryFoodContactCommand(foodItemId.Value.ToString()));
        session.Food.Consume(foodItemId, FoodType.Ordinary);
        HandleOrdinaryFoodContactResult consumedResult = useCase.Execute(
            new HandleOrdinaryFoodContactCommand(foodItemId.Value.ToString()));

        Assert.True(nonHeadResult.IsSuccess);
        Assert.True(consumedResult.IsSuccess);
        Assert.Equal(3, session.Snake.Length);
        Assert.Empty(session.PublishedFacts);
    }
}
