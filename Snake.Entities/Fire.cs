namespace Snake.Entities;

public class Fire
{
    private FireId _fireId;
    private FireItem? _activeItem;
    private bool _pendingSpawn;

    public Fire(FireId fireId)
    {
        _fireId = fireId ?? throw new ArgumentNullException(nameof(fireId));
    }

    public FireId FireId => _fireId;
    public FireItem? ActiveItem => _activeItem;
    public bool PendingSpawn => _pendingSpawn;
    public int LengthDelta => -2;

    public void Spawn(FireItemId fireItemId, GridPosition position)
    {
        if (_activeItem is not null)
        {
            return;
        }

        _activeItem = new FireItem(fireItemId, position);
        _pendingSpawn = false;
    }

    public bool Consume(FireItemId fireItemId)
    {
        ArgumentNullException.ThrowIfNull(fireItemId);
        if (_activeItem is null || !_activeItem.FireItemId.Equals(fireItemId))
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
