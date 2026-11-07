namespace Snake.Domain
{

public class BombItem
{
    private GridPosition _position;

    public BombItem(GridPosition position)
    {
        _position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public GridPosition Position => _position;
}
}
