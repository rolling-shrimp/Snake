namespace Snake.Entities;

public class GridPosition
{
    private int _x;
    private int _y;

    public GridPosition(int x, int y)
    {
        if (x < 0 || x > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "X 必須介於 0 至 9。");
        }

        if (y < 0 || y > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Y 必須介於 0 至 9。");
        }

        _x = x;
        _y = y;
    }

    public int X => _x;
    public int Y => _y;

    public override bool Equals(object? value)
    {
        if (value is not GridPosition position)
        {
            return false;
        }

        return _x == position._x && _y == position._y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_x, _y);
    }
}
