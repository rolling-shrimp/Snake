namespace Snake.UseCases;

public class HandleSpecialFoodContactCommand
{
    public HandleSpecialFoodContactCommand(string foodItemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(foodItemId);
        FoodItemId = foodItemId;
    }

    public string FoodItemId { get; }
}
