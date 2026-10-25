using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class HandleSpecialFoodContactAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-SPECIAL-FOOD-CONTACT-001")]
    public void SCN_HANDLE_SPECIAL_FOOD_CONTACT_001_ConsumesFoodAndGrowsExactlyTwo()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second);
        FoodItemId foodItemId = new FoodItemId(Guid.NewGuid());
        session.Food.SpawnSpecial(foodItemId, session.Snake.BodyCells[0]);
        HandleSpecialFoodContactUseCase useCase = new HandleSpecialFoodContactUseCase(session);

        HandleSpecialFoodContactResult result = useCase.Execute(
            new HandleSpecialFoodContactCommand(foodItemId.Value.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Empty(session.Food.ActiveItems);
        Assert.Equal(5, session.Snake.Length);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-SPECIAL-FOOD-CONTACT-002")]
    public void SCN_HANDLE_SPECIAL_FOOD_CONTACT_002_PreservesFiftyOneAcrossThirdLevelTarget()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third, 49);
        FoodItemId foodItemId = new FoodItemId(Guid.NewGuid());
        session.Food.SpawnSpecial(foodItemId, session.Snake.BodyCells[0]);
        HandleSpecialFoodContactUseCase useCase = new HandleSpecialFoodContactUseCase(session);

        HandleSpecialFoodContactResult result = useCase.Execute(
            new HandleSpecialFoodContactCommand(foodItemId.Value.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Equal(51, session.Snake.Length);
        SnakeLengthChangedDomainEvent lengthChanged = Assert.Single(
            session.PublishedFacts.OfType<SnakeLengthChangedDomainEvent>());
        Assert.Equal(51, lengthChanged.CurrentLength);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-SPECIAL-FOOD-CONTACT-003")]
    public void SCN_HANDLE_SPECIAL_FOOD_CONTACT_003_IgnoresFirstLevelAndInactiveItem()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First);
        FoodItemId foodItemId = new FoodItemId(Guid.NewGuid());
        session.Food.SpawnSpecial(foodItemId, session.Snake.BodyCells[0]);
        HandleSpecialFoodContactUseCase useCase = new HandleSpecialFoodContactUseCase(session);

        HandleSpecialFoodContactResult firstLevelResult = useCase.Execute(
            new HandleSpecialFoodContactCommand(foodItemId.Value.ToString()));
        HandleSpecialFoodContactResult inactiveResult = useCase.Execute(
            new HandleSpecialFoodContactCommand(Guid.NewGuid().ToString()));

        Assert.True(firstLevelResult.IsSuccess);
        Assert.True(inactiveResult.IsSuccess);
        Assert.Equal(3, session.Snake.Length);
        Assert.Single(session.Food.ActiveItems);
    }
}
