namespace Snake.Entities;

public class Shield
{
    private const int MAX_SPAWNED_PER_LEVEL = 2;

    private ShieldId _shieldId;
    private ShieldItem? _activeItem;
    private bool _pendingSpawn;
    private int _remainingCharges;
    private int _spawnedCount;

    public Shield(ShieldId shieldId)
    {
        _shieldId = shieldId ?? throw new ArgumentNullException(nameof(shieldId));
    }

    public ShieldId ShieldId => _shieldId;
    public ShieldItem? ActiveItem => _activeItem;
    public bool PendingSpawn => _pendingSpawn;
    public int RemainingCharges => _remainingCharges;
    public int SpawnedCount => _spawnedCount;
    public bool CanRescue => _remainingCharges > 0;

    public void Spawn(ShieldItemId shieldItemId, GridPosition position)
    {
        if (_activeItem is not null || _spawnedCount >= MAX_SPAWNED_PER_LEVEL)
        {
            return;
        }

        _activeItem = new ShieldItem(shieldItemId, position);
        _spawnedCount++;
        _pendingSpawn = false;
    }

    public bool Collect(ShieldItemId shieldItemId)
    {
        ArgumentNullException.ThrowIfNull(shieldItemId);
        if (_activeItem is null || !_activeItem.ShieldItemId.Equals(shieldItemId))
        {
            return false;
        }

        _activeItem = null;
        _remainingCharges++;
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
        _remainingCharges = 0;
        _spawnedCount = 0;
    }

    public bool UseCharge()
    {
        if (_remainingCharges == 0)
        {
            return false;
        }

        _remainingCharges--;
        return true;
    }

    public void MarkPending()
    {
        if (_spawnedCount < MAX_SPAWNED_PER_LEVEL)
        {
            _pendingSpawn = true;
        }
    }

    public void ClearPending()
    {
        _pendingSpawn = false;
    }
}
