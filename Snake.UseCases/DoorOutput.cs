namespace Snake.UseCases;

public class DoorOutput
{
    public DoorOutput(string doorId, GridPositionDTO position, DoorVisualState state)
    {
        if (string.IsNullOrWhiteSpace(doorId))
        {
            throw new ArgumentException("門識別碼不得為空。", nameof(doorId));
        }

        DoorId = doorId;
        Position = position ?? throw new ArgumentNullException(nameof(position));
        State = state;
    }

    public string DoorId { get; }
    public GridPositionDTO Position { get; }
    public DoorVisualState State { get; }
}
