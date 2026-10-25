namespace Snake.Entities;

public class ShieldId
{
    private Guid _value;

    public ShieldId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("ShieldId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
