namespace Snake.Entities;

public class FoodConsumedDomainEvent : IDomainEvent
{
    public FoodConsumedDomainEvent(FoodItemId foodItemId, FoodType foodType)
    {
        FoodItemId = foodItemId ?? throw new ArgumentNullException(nameof(foodItemId));
        FoodType = foodType;
    }

    public FoodItemId FoodItemId { get; }
    public FoodType FoodType { get; }
}
