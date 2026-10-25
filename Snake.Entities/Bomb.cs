namespace Snake.Entities;

public class Bomb
{
    private BombId _bombId;
    private BombItem? _activeItem;
    private bool _pendingSpawn;

    public Bomb(BombId bombId)
    {
        _bombId = bombId ?? throw new ArgumentNullException(nameof(bombId));
    }

    public BombId BombId => _bombId;
    public BombItem? ActiveItem => _activeItem;
    public bool PendingSpawn => _pendingSpawn;

    public void Spawn(BombItemId bombItemId, GridPosition position)
    {
        if (_activeItem is not null)
        {
            return;
        }

        _activeItem = new BombItem(bombItemId, position);
        _pendingSpawn = false;
    }

    public bool Consume(BombItemId bombItemId)
    {
        ArgumentNullException.ThrowIfNull(bombItemId);
        if (_activeItem is null || !_activeItem.BombItemId.Equals(bombItemId))
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
