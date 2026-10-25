namespace Snake.Entities;

public class LevelId
{
    private Guid _value;

    public LevelId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("LevelId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
