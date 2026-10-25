namespace Snake.Entities;

public class DoorId
{
    private Guid _value;

    public DoorId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("DoorId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;

    public override bool Equals(object? value)
    {
        return value is DoorId doorId && _value == doorId._value;
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }
}
