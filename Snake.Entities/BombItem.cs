namespace Snake.Entities;

public class BombItem
{
    private BombItemId _bombItemId;
    private GridPosition _position;

    public BombItem(BombItemId bombItemId, GridPosition position)
    {
        _bombItemId = bombItemId ?? throw new ArgumentNullException(nameof(bombItemId));
        _position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public BombItemId BombItemId => _bombItemId;
    public GridPosition Position => _position;
}
