namespace Snake.Entities;

public class ShieldItem
{
    private ShieldItemId _shieldItemId;
    private GridPosition _position;

    public ShieldItem(ShieldItemId shieldItemId, GridPosition position)
    {
        _shieldItemId = shieldItemId ?? throw new ArgumentNullException(nameof(shieldItemId));
        _position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public ShieldItemId ShieldItemId => _shieldItemId;
    public GridPosition Position => _position;
}
