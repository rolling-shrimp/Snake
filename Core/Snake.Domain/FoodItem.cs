namespace Snake.Domain
{

public class FoodItem
{
    private FoodType _type;
    private GridPosition _position;

    public FoodItem(FoodType type, GridPosition position)
    {
        _type = type;
        _position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public FoodType Type => _type;
    public GridPosition Position => _position;
}
}
