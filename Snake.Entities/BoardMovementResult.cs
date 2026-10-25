namespace Snake.Entities;

public class BoardMovementResult
{
    public BoardMovementResult(GridPosition? position, int attemptedX, int attemptedY)
    {
        Position = position;
        AttemptedX = attemptedX;
        AttemptedY = attemptedY;
    }

    public GridPosition? Position { get; }
    public int AttemptedX { get; }
    public int AttemptedY { get; }
}
