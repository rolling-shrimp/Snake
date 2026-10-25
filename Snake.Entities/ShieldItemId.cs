namespace Snake.Entities;

public class ShieldItemId
{
    private Guid _value;

    public ShieldItemId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("ShieldItemId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;

    public override bool Equals(object? value)
    {
        return value is ShieldItemId shieldItemId && _value == shieldItemId._value;
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }
}
