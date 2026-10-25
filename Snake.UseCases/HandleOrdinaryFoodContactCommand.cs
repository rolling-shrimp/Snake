namespace Snake.UseCases;

public class HandleOrdinaryFoodContactCommand
{
    public HandleOrdinaryFoodContactCommand(string foodItemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(foodItemId);
        FoodItemId = foodItemId;
    }

    public string FoodItemId { get; }
}
