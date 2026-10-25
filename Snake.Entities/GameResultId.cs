namespace Snake.Entities;

public class GameResultId
{
    private Guid _value;

    public GameResultId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("GameResultId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
