namespace Snake.Entities;

public class FireItemId
{
    private Guid _value;

    public FireItemId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("FireItemId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;

    public override bool Equals(object? value)
    {
        return value is FireItemId fireItemId && _value == fireItemId._value;
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }
}
