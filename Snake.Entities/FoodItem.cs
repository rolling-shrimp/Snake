namespace Snake.Entities;

public class FoodItem
{
    private FoodItemId _foodItemId;
    private FoodType _type;
    private GridPosition _position;

    public FoodItem(FoodItemId foodItemId, FoodType type, GridPosition position)
    {
        _foodItemId = foodItemId ?? throw new ArgumentNullException(nameof(foodItemId));
        _type = type;
        _position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public FoodItemId FoodItemId => _foodItemId;
    public FoodType Type => _type;
    public GridPosition Position => _position;
}
