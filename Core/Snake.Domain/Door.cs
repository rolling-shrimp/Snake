namespace Snake.Domain
{

public class Door
{
    private GridPosition _position;
    private DoorPhase _phase;

    public Door(GridPosition position)
    {
        _position = position ?? throw new ArgumentNullException(nameof(position));
        _phase = DoorPhase.Open;
    }

    public GridPosition Position => _position;
    public DoorPhase Phase => _phase;
    public bool IsPassable => _phase != DoorPhase.Closed;

    internal Door Copy()
    {
        Door copy = new Door(_position);
        copy._phase = _phase;
        return copy;
    }

    public void ChangePhase(DoorPhase phase)
    {
        _phase = phase;
    }
}
}
