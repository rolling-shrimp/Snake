namespace Snake.Entities;

public class BombItemId
{
    private Guid _value;

    public BombItemId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("BombItemId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;

    public override bool Equals(object? value)
    {
        return value is BombItemId bombItemId && _value == bombItemId._value;
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }
}
