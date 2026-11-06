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
        if (safeStartCells.Count == 0 || safeStartCells.Distinct().Count() != safeStartCells.Count)
        {
            throw new ArgumentException("蛇身必須包含至少一個且不得重疊的格位。", nameof(safeStartCells));
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

    internal Snake Copy()
    {
        Snake copy = new Snake(_snakeId, new List<GridPosition>(_bodyCells));
        copy._currentDirection = _currentDirection;
        copy._queuedDirection = _queuedDirection;
        return copy;
    }

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

    public void ResetSafely(
        List<GridPosition> safeCells,
        Board board,
        BoundaryPolicy boundaryPolicy)
    {
        ArgumentNullException.ThrowIfNull(safeCells);
        ArgumentNullException.ThrowIfNull(board);
        if (safeCells.Count == 0
            || safeCells.Distinct().Count() != safeCells.Count
            || !board.IsContinuous(safeCells, boundaryPolicy))
        {
            throw new InvalidOperationException("安全蛇身未滿足順序、唯一與連續不變式。");
        }

        _bodyCells = new List<GridPosition>(safeCells);
        _currentDirection = Direction.Right;
        _queuedDirection = null;
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

    public SnakeMovementPlanResult PlanMovement(
        GridPosition nextHead,
        int growthDelta,
        Board board,
        BoundaryPolicy boundaryPolicy,
        List<GridPosition> blockedCells)
    {
        ArgumentNullException.ThrowIfNull(nextHead);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(blockedCells);
        if (growthDelta < 0 || growthDelta > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(growthDelta));
        }

        List<GridPosition> selfCollisionCells = growthDelta == 0
            ? _bodyCells.Take(_bodyCells.Count - 1).ToList()
            : new List<GridPosition>(_bodyCells);
        List<GridPosition> resultingBodyCells = new List<GridPosition>
        {
            nextHead
        };
        if (growthDelta == 0)
        {
            resultingBodyCells.AddRange(_bodyCells.Take(_bodyCells.Count - 1));
        }
        else
        {
            resultingBodyCells.AddRange(_bodyCells);
        }

        if (growthDelta == 2)
        {
            HashSet<GridPosition> unavailableCells = new HashSet<GridPosition>(resultingBodyCells);
            unavailableCells.UnionWith(blockedCells);
            GridPosition? tailExtension = board
                .GetTailExtensionCandidates(_bodyCells, boundaryPolicy)
                .FirstOrDefault(candidate => !unavailableCells.Contains(candidate));
            if (tailExtension is null)
            {
                return new SnakeMovementPlanResult(
                    false,
                    null,
                    "SnakeTopologyUnavailable",
                    "特殊食物沒有合法的尾端延伸格位。");
            }

            resultingBodyCells.Add(tailExtension);
        }

        SnakeMovementPlan plan = new SnakeMovementPlan(
            nextHead,
            growthDelta,
            growthDelta == 0,
            selfCollisionCells,
            resultingBodyCells);
        return new SnakeMovementPlanResult(true, plan, null, null);
    }

    public void CommitMovement(
        SnakeMovementPlan plan,
        Board board,
        BoundaryPolicy boundaryPolicy)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(board);
        List<GridPosition> resultingBodyCells = plan.ResultingBodyCells;
        if (resultingBodyCells.Count == 0
            || resultingBodyCells.Distinct().Count() != resultingBodyCells.Count
            || !board.IsContinuous(resultingBodyCells, boundaryPolicy))
        {
            throw new InvalidOperationException("移動計畫未滿足蛇身順序、唯一與連續不變式。");
        }

        _bodyCells = resultingBodyCells;
        _currentDirection = NextDirection;
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
