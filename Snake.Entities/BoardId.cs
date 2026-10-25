namespace Snake.Entities;

public class BoardId
{
    private Guid _value;

    public BoardId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("BoardId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
