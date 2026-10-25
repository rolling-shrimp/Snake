namespace Snake.WinForms;

public class DoorViewModel
{
    public DoorViewModel(Guid doorId, GridCellViewModel position, string state)
    {
        DoorId = doorId;
        Position = position ?? throw new ArgumentNullException(nameof(position));
        State = state;
    }

    public Guid DoorId { get; }
    public GridCellViewModel Position { get; }
    public string State { get; }
}
