namespace Snake.Domain
{

public class FireItem
{
    private GridPosition _position;

    public FireItem(GridPosition position)
    {
        _position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public GridPosition Position => _position;
}
}
