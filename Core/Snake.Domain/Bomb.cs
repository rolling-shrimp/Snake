namespace Snake.Domain
{

public class Bomb
{
    private BombItem? _activeItem;
    private bool _pendingSpawn;

    public Bomb()
    {
    }

    public BombItem? ActiveItem => _activeItem;
    public bool PendingSpawn => _pendingSpawn;

    internal Bomb Copy()
    {
        Bomb copy = new Bomb();
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

        _activeItem = new BombItem(position);
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
