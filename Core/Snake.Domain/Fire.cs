namespace Snake.Domain
{

public class Fire
{
    private FireItem? _activeItem;
    private bool _pendingSpawn;

    public Fire()
    {
    }

    public FireItem? ActiveItem => _activeItem;
    public bool PendingSpawn => _pendingSpawn;
    public int LengthDelta => -2;

    internal Fire Copy()
    {
        Fire copy = new Fire();
        copy._activeItem = _activeItem;
        copy._pendingSpawn = _pendingSpawn;
        return copy;
    }

    public void Spawn(GridPosition position)
    {
        if (_activeItem is not null)
        {
            return;
        }

        _activeItem = new FireItem(position);
        _pendingSpawn = false;
    }

    public bool Consume()
    {
        if (_activeItem is null)
        {
            return false;
        }

        _activeItem = null;
        return true;
    }

    public List<GridPosition> GetOccupiedCells()
    {
        return _activeItem is null
            ? new List<GridPosition>()
            : new List<GridPosition> { _activeItem.Position };
    }

    public void Reset()
    {
        _activeItem = null;
        _pendingSpawn = false;
    }

    public void MarkPending()
    {
        _pendingSpawn = true;
    }

    public void ClearPending()
    {
        _pendingSpawn = false;
    }
}
}
