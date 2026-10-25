namespace Snake.Entities;

public class FoodId
{
    private Guid _value;

    public FoodId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("FoodId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
