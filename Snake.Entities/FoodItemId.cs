namespace Snake.Entities;

public class FoodItemId
{
    private Guid _value;

    public FoodItemId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("FoodItemId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;

    public override bool Equals(object? value)
    {
        return value is FoodItemId foodItemId && _value == foodItemId._value;
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }
}
