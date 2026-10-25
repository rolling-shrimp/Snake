namespace Snake.Entities;

public class Food
{
    private FoodId _foodId;
    private List<FoodItem> _activeItems;
    private List<FoodType> _pendingTypes;

    public Food(FoodId foodId)
    {
        _foodId = foodId ?? throw new ArgumentNullException(nameof(foodId));
        _activeItems = new List<FoodItem>();
        _pendingTypes = new List<FoodType>();
    }

    public FoodId FoodId => _foodId;
    public List<FoodItem> ActiveItems => new List<FoodItem>(_activeItems);
    public List<FoodType> PendingTypes => new List<FoodType>(_pendingTypes);
    public int OrdinaryLengthDelta => 1;
    public int SpecialLengthDelta => 2;

    public void SpawnOrdinary(FoodItemId foodItemId, GridPosition position)
    {
        if (_activeItems.Any(item => item.Type == FoodType.Ordinary))
        {
            return;
        }

        _activeItems.Add(new FoodItem(foodItemId, FoodType.Ordinary, position));
    }

    public void SpawnSpecial(FoodItemId foodItemId, GridPosition position)
    {
        if (_activeItems.Any(item => item.Type == FoodType.Special))
        {
            return;
        }

        _activeItems.Add(new FoodItem(foodItemId, FoodType.Special, position));
    }

    public bool Consume(FoodItemId foodItemId, FoodType foodType)
    {
        ArgumentNullException.ThrowIfNull(foodItemId);

        FoodItem? item = _activeItems.FirstOrDefault(activeItem =>
            activeItem.FoodItemId.Equals(foodItemId) && activeItem.Type == foodType);
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
}
