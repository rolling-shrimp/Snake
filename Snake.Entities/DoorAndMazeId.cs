namespace Snake.Entities;

public class DoorAndMazeId
{
    private Guid _value;

    public DoorAndMazeId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("DoorAndMazeId 不得為空。", nameof(value));
        }

        _value = value;
    }

    public Guid Value => _value;
}
