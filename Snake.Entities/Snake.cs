namespace Snake.Entities;

public class Snake
{
    private const int MOVEMENT_INTERVAL_MILLISECONDS = 250;

    private SnakeId _snakeId;
    private List<GridPosition> _bodyCells;
    private Direction _currentDirection;
    private Direction? _queuedDirection;

    public Snake(SnakeId snakeId, List<GridPosition> safeStartCells)
    {
        _snakeId = snakeId ?? throw new ArgumentNullException(nameof(snakeId));
        ArgumentNullException.ThrowIfNull(safeStartCells);
        if (safeStartCells.Count != 3 || safeStartCells.Distinct().Count() != 3)
        {
            throw new ArgumentException("第一關安全起點必須包含三個不重疊格位。", nameof(safeStartCells));
        }

        _bodyCells = new List<GridPosition>(safeStartCells);
        _currentDirection = Direction.Right;
        _queuedDirection = null;
    }

    public SnakeId SnakeId => _snakeId;
    public List<GridPosition> BodyCells => new List<GridPosition>(_bodyCells);
    public int Length => _bodyCells.Count;
    public Direction CurrentDirection => _currentDirection;
    public Direction? QueuedDirection => _queuedDirection;
    public Direction NextDirection => _queuedDirection ?? _currentDirection;
    public int MovementIntervalMilliseconds => MOVEMENT_INTERVAL_MILLISECONDS;
    public int? LevelTimeLimitMilliseconds => null;
    public bool IsSelfCollisionFatal => true;

    public bool QueueDirection(Direction direction)
    {
        if (direction != Direction.Up
            && direction != Direction.Down
            && direction != Direction.Left
            && direction != Direction.Right)
        {
            return false;
        }

        if (IsOppositeDirection(direction))
        {
            return false;
        }

        _queuedDirection = direction;
        return true;
    }

    public void MoveTo(GridPosition nextHead)
    {
        ArgumentNullException.ThrowIfNull(nextHead);

        _bodyCells.Insert(0, nextHead);
        _bodyCells.RemoveAt(_bodyCells.Count - 1);
        _currentDirection = NextDirection;
        _queuedDirection = null;
    }

    public void Reset(List<GridPosition> safeCells)
    {
        ArgumentNullException.ThrowIfNull(safeCells);
        if (safeCells.Distinct().Count() != safeCells.Count)
        {
            throw new ArgumentException("安全蛇身格位不得重疊。", nameof(safeCells));
        }

        _bodyCells = new List<GridPosition>(safeCells);
        _currentDirection = Direction.Right;
        _queuedDirection = null;
    }

    public void Grow(List<GridPosition> addedTailCells)
    {
        ArgumentNullException.ThrowIfNull(addedTailCells);
        if (addedTailCells.Count == 0)
        {
            return;
        }

        HashSet<GridPosition> occupiedCells = new HashSet<GridPosition>(_bodyCells);
        if (addedTailCells.Any(cell => !occupiedCells.Add(cell)))
        {
            throw new ArgumentException("新增蛇身格位不得與目前蛇身或彼此重疊。", nameof(addedTailCells));
        }

        _bodyCells.AddRange(addedTailCells);
    }

    public LengthChangeResult CalculateLengthChange(int lengthDelta)
    {
        int effectiveLength = Math.Max(0, Length + lengthDelta);
        return new LengthChangeResult(
            Length,
            lengthDelta,
            effectiveLength,
            effectiveLength == 0);
    }

    public void CommitReduction(LengthChangeResult lengthChange)
    {
        ArgumentNullException.ThrowIfNull(lengthChange);
        if (lengthChange.PreviousLength != Length
            || lengthChange.RequestedDelta >= 0
            || lengthChange.IsDepleted)
        {
            throw new InvalidOperationException("只有目前蛇身的非致死減長結果可以提交。");
        }

        _bodyCells.RemoveRange(
            lengthChange.EffectiveLength,
            _bodyCells.Count - lengthChange.EffectiveLength);
    }

    public void Deplete()
    {
        _bodyCells.Clear();
        _queuedDirection = null;
    }

    private bool IsOppositeDirection(Direction direction)
    {
        return (_currentDirection == Direction.Up && direction == Direction.Down)
            || (_currentDirection == Direction.Down && direction == Direction.Up)
            || (_currentDirection == Direction.Left && direction == Direction.Right)
            || (_currentDirection == Direction.Right && direction == Direction.Left);
    }
}
