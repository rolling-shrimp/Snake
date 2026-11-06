namespace Snake.UseCases;

public class GridPositionDTO
{
    public GridPositionDTO(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }

    public override bool Equals(object? value)
    {
        return value is GridPositionDTO position && X == position.X && Y == position.Y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}
