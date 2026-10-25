namespace Snake.Entities;

public class Board
{
    private const int ROWS = 10;
    private const int COLUMNS = 10;

    private BoardId _boardId;

    public Board(BoardId boardId)
    {
        _boardId = boardId ?? throw new ArgumentNullException(nameof(boardId));
    }

    public BoardId BoardId => _boardId;
    public int Rows => ROWS;
    public int Columns => COLUMNS;
    public int TotalCells => ROWS * COLUMNS;

    public List<GridPosition> GetFirstLevelSafeStart()
    {
        return new List<GridPosition>
        {
            new GridPosition(3, 1),
            new GridPosition(2, 1),
            new GridPosition(1, 1)
        };
    }

    public BoardMovementResult ResolveMovement(
        GridPosition currentHead,
        Direction direction,
        BoundaryPolicy boundaryPolicy)
    {
        ArgumentNullException.ThrowIfNull(currentHead);

        int attemptedX = currentHead.X;
        int attemptedY = currentHead.Y;
        switch (direction)
        {
            case Direction.Up:
                attemptedY--;
                break;
            case Direction.Down:
                attemptedY++;
                break;
            case Direction.Left:
                attemptedX--;
                break;
            case Direction.Right:
                attemptedX++;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "不支援的移動方向。");
        }

        bool isOutside = attemptedX < 0
            || attemptedX >= COLUMNS
            || attemptedY < 0
            || attemptedY >= ROWS;
        if (isOutside && boundaryPolicy == BoundaryPolicy.Blocked)
        {
            return new BoardMovementResult(null, attemptedX, attemptedY);
        }

        int resolvedX = (attemptedX + COLUMNS) % COLUMNS;
        int resolvedY = (attemptedY + ROWS) % ROWS;
        return new BoardMovementResult(
            new GridPosition(resolvedX, resolvedY),
            attemptedX,
            attemptedY);
    }

    public List<GridPosition> GetLegalSpawnCells(List<GridPosition> snakeCells, List<GridPosition> objectCells)
    {
        ArgumentNullException.ThrowIfNull(snakeCells);
        ArgumentNullException.ThrowIfNull(objectCells);

        HashSet<GridPosition> occupiedCells = new HashSet<GridPosition>(snakeCells);
        occupiedCells.UnionWith(objectCells);
        List<GridPosition> legalCells = new List<GridPosition>();
        for (int y = 0; y < ROWS; y++)
        {
            for (int x = 0; x < COLUMNS; x++)
            {
                GridPosition position = new GridPosition(x, y);
                if (!occupiedCells.Contains(position))
                {
                    legalCells.Add(position);
                }
            }
        }

        return legalCells;
    }
}
