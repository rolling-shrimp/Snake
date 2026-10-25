namespace Snake.Entities;

public class GameId
{
    private Guid _value;

    public GameId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("GameId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
