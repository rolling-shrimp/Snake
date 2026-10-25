namespace Snake.Entities;

public class BombId
{
    private Guid _value;

    public BombId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("BombId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
