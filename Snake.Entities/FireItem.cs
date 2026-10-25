namespace Snake.Entities;

public class FireItem
{
    private FireItemId _fireItemId;
    private GridPosition _position;

    public FireItem(FireItemId fireItemId, GridPosition position)
    {
        _fireItemId = fireItemId ?? throw new ArgumentNullException(nameof(fireItemId));
        _position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public FireItemId FireItemId => _fireItemId;
    public GridPosition Position => _position;
}
