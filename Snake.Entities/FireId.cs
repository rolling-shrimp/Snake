namespace Snake.Entities;

public class FireId
{
    private Guid _value;

    public FireId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("FireId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
