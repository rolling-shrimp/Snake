namespace Snake.Domain
{

public class Food
{
    private List<FoodItem> _activeItems;
    private List<FoodType> _pendingTypes;

    public Food()
    {
        _activeItems = new List<FoodItem>();
        _pendingTypes = new List<FoodType>();
    }

    public List<FoodItem> ActiveItems => new List<FoodItem>(_activeItems);
    public List<FoodType> PendingTypes => new List<FoodType>(_pendingTypes);
    public int OrdinaryLengthDelta => 1;
    public int SpecialLengthDelta => 2;

    internal Food Copy()
    {
        Food copy = new Food();
        copy._activeItems = new List<FoodItem>(_activeItems);
        copy._pendingTypes = new List<FoodType>(_pendingTypes);
        return copy;
    }

    public void SpawnOrdinary(GridPosition position)
    {
        if (_activeItems.Any(item => item.Type == FoodType.Ordinary))
        {
            return;
        }

        _activeItems.Add(new FoodItem(FoodType.Ordinary, position));
    }

    public void SpawnSpecial(GridPosition position)
    {
        if (_activeItems.Any(item => item.Type == FoodType.Special))
        {
            return;
        }

        _activeItems.Add(new FoodItem(FoodType.Special, position));
    }

    public bool Consume(FoodType foodType)
    {
        FoodItem? item = _activeItems.FirstOrDefault(activeItem => activeItem.Type == foodType);
        if (item is null)
        {
            return false;
        }

        _activeItems.Remove(item);
        return true;
    }

    public List<GridPosition> GetOccupiedCells()
    {
        return _activeItems.Select(item => item.Position).ToList();
    }

    public void Reset()
    {
        _activeItems.Clear();
        _pendingTypes.Clear();
    }

    public void MarkPending(FoodType foodType)
    {
        if (!_pendingTypes.Contains(foodType))
        {
            _pendingTypes.Add(foodType);
        }
    }

    public void ClearPending()
    {
        _pendingTypes.Clear();
    }

    public void RemovePending(FoodType foodType)
    {
        _pendingTypes.Remove(foodType);
    }
}
}
