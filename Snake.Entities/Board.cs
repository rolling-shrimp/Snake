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

    internal Board Copy()
    {
        return new Board(_boardId);
    }

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

    public bool AreAdjacent(
        GridPosition first,
        GridPosition second,
        BoundaryPolicy boundaryPolicy)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        int xDistance = Math.Abs(first.X - second.X);
        int yDistance = Math.Abs(first.Y - second.Y);
        if (xDistance + yDistance == 1)
        {
            return true;
        }

        if (boundaryPolicy != BoundaryPolicy.Wrap)
        {
            return false;
        }

        bool wrapsHorizontally = first.Y == second.Y && xDistance == COLUMNS - 1;
        bool wrapsVertically = first.X == second.X && yDistance == ROWS - 1;
        return wrapsHorizontally || wrapsVertically;
    }

    public bool IsContinuous(List<GridPosition> bodyCells, BoundaryPolicy boundaryPolicy)
    {
        ArgumentNullException.ThrowIfNull(bodyCells);
        for (int index = 1; index < bodyCells.Count; index++)
        {
            if (!AreAdjacent(bodyCells[index - 1], bodyCells[index], boundaryPolicy))
            {
                return false;
            }
        }

        return true;
    }

    public List<GridPosition> GetTailExtensionCandidates(
        List<GridPosition> bodyCells,
        BoundaryPolicy boundaryPolicy)
    {
        ArgumentNullException.ThrowIfNull(bodyCells);
        if (bodyCells.Count < 2)
        {
            return new List<GridPosition>();
        }

        GridPosition tail = bodyCells[^1];
        GridPosition beforeTail = bodyCells[^2];
        int xDirection = GetWrappedDirection(beforeTail.X, tail.X, COLUMNS, boundaryPolicy);
        int yDirection = GetWrappedDirection(beforeTail.Y, tail.Y, ROWS, boundaryPolicy);
        List<(int X, int Y)> rawCandidates = new List<(int X, int Y)>
        {
            (tail.X + xDirection, tail.Y + yDirection),
            (tail.X + yDirection, tail.Y - xDirection),
            (tail.X - yDirection, tail.Y + xDirection)
        };
        List<GridPosition> candidates = new List<GridPosition>();
        foreach ((int x, int y) in rawCandidates)
        {
            GridPosition? candidate = ResolveCandidate(x, y, boundaryPolicy);
            if (candidate is not null && !candidates.Contains(candidate))
            {
                candidates.Add(candidate);
            }
        }

        return candidates;
    }

    public List<GridPosition>? FindSafeBody(
        int requiredLength,
        BoundaryPolicy boundaryPolicy,
        List<GridPosition> blockedCells)
    {
        if (requiredLength <= 0 || requiredLength > TotalCells)
        {
            return null;
        }

        ArgumentNullException.ThrowIfNull(blockedCells);
        HashSet<GridPosition> blockedCellSet = new HashSet<GridPosition>(blockedCells);
        for (int y = 0; y < ROWS; y++)
        {
            for (int x = 0; x < COLUMNS; x++)
            {
                GridPosition start = new GridPosition(x, y);
                if (blockedCellSet.Contains(start))
                {
                    continue;
                }

                BoardMovementResult nextMovement = ResolveMovement(
                    start,
                    Direction.Right,
                    boundaryPolicy);
                if (nextMovement.Position is null
                    || blockedCellSet.Contains(nextMovement.Position))
                {
                    continue;
                }

                List<GridPosition> path = new List<GridPosition> { start };
                HashSet<GridPosition> visited = new HashSet<GridPosition>
                {
                    start,
                    nextMovement.Position
                };
                if (TryBuildSafePath(
                    path,
                    visited,
                    blockedCellSet,
                    requiredLength,
                    boundaryPolicy))
                {
                    return path;
                }
            }
        }

        return null;
    }

    private bool TryBuildSafePath(
        List<GridPosition> path,
        HashSet<GridPosition> visited,
        HashSet<GridPosition> blockedCells,
        int requiredLength,
        BoundaryPolicy boundaryPolicy)
    {
        if (path.Count == requiredLength)
        {
            return true;
        }

        foreach (GridPosition neighbor in GetNeighbors(path[^1], boundaryPolicy))
        {
            if (blockedCells.Contains(neighbor) || !visited.Add(neighbor))
            {
                continue;
            }

            path.Add(neighbor);
            if (TryBuildSafePath(path, visited, blockedCells, requiredLength, boundaryPolicy))
            {
                return true;
            }

            path.RemoveAt(path.Count - 1);
            visited.Remove(neighbor);
        }

        return false;
    }

    private List<GridPosition> GetNeighbors(
        GridPosition position,
        BoundaryPolicy boundaryPolicy)
    {
        List<Direction> directions = new List<Direction>
        {
            Direction.Right,
            Direction.Down,
            Direction.Left,
            Direction.Up
        };
        List<GridPosition> neighbors = new List<GridPosition>();
        foreach (Direction direction in directions)
        {
            BoardMovementResult movement = ResolveMovement(position, direction, boundaryPolicy);
            if (movement.Position is not null && !neighbors.Contains(movement.Position))
            {
                neighbors.Add(movement.Position);
            }
        }

        return neighbors;
    }

    private int GetWrappedDirection(
        int firstCoordinate,
        int secondCoordinate,
        int dimension,
        BoundaryPolicy boundaryPolicy)
    {
        int difference = secondCoordinate - firstCoordinate;
        if (boundaryPolicy == BoundaryPolicy.Wrap && Math.Abs(difference) == dimension - 1)
        {
            return difference > 0 ? -1 : 1;
        }

        return difference;
    }

    private GridPosition? ResolveCandidate(int x, int y, BoundaryPolicy boundaryPolicy)
    {
        bool isOutside = x < 0 || x >= COLUMNS || y < 0 || y >= ROWS;
        if (isOutside && boundaryPolicy == BoundaryPolicy.Blocked)
        {
            return null;
        }

        int resolvedX = (x + COLUMNS) % COLUMNS;
        int resolvedY = (y + ROWS) % ROWS;
        return new GridPosition(resolvedX, resolvedY);
    }
}
