namespace Snake.Entities;

public class LevelResultId
{
    private Guid _value;

    public LevelResultId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("LevelResultId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
