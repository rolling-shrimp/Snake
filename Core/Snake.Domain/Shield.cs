namespace Snake.Domain
{

public class Shield
{
    private const int MAX_SPAWNED_PER_LEVEL = 2;

    private ShieldItem? _activeItem;
    private bool _pendingSpawn;
    private int _remainingCharges;
    private int _spawnedCount;

    public Shield()
    {
    }

    public ShieldItem? ActiveItem => _activeItem;
    public bool PendingSpawn => _pendingSpawn;
    public int RemainingCharges => _remainingCharges;
    public int SpawnedCount => _spawnedCount;
    public bool CanRescue => _remainingCharges > 0;

    internal Shield Copy()
    {
        Shield copy = new Shield();
        copy._activeItem = _activeItem;
        copy._pendingSpawn = _pendingSpawn;
        copy._remainingCharges = _remainingCharges;
        copy._spawnedCount = _spawnedCount;
        return copy;
    }

    public void Spawn(GridPosition position)
    {
        if (_activeItem is not null || _spawnedCount >= MAX_SPAWNED_PER_LEVEL)
        {
            return;
        }

        _activeItem = new ShieldItem(position);
        _spawnedCount++;
        _pendingSpawn = false;
    }

    public bool Collect()
    {
        if (_activeItem is null)
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
}
