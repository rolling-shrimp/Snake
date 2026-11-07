namespace Snake.Domain
{

public class ShieldItem
{
    private GridPosition _position;

    public ShieldItem(GridPosition position)
    {
        _position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public GridPosition Position => _position;
}
}
