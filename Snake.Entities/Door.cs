namespace Snake.Entities;

public class Door
{
    private DoorId _doorId;
    private GridPosition _position;
    private DoorPhase _phase;

    public Door(DoorId doorId, GridPosition position)
    {
        _doorId = doorId ?? throw new ArgumentNullException(nameof(doorId));
        _position = position ?? throw new ArgumentNullException(nameof(position));
        _phase = DoorPhase.Open;
    }

    public DoorId DoorId => _doorId;
    public GridPosition Position => _position;
    public DoorPhase Phase => _phase;
    public bool IsPassable => _phase != DoorPhase.Closed;

    public void ChangePhase(DoorPhase phase)
    {
        _phase = phase;
    }
}
