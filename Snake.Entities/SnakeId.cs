namespace Snake.Entities;

public class SnakeId
{
    private Guid _value;

    public SnakeId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("SnakeId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
