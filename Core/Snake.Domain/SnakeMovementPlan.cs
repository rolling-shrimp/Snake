namespace Snake.Domain
{

public class SnakeMovementPlan
{
    private List<GridPosition> _selfCollisionCells;
    private List<GridPosition> _resultingBodyCells;

    public SnakeMovementPlan(
        GridPosition nextHead,
        int growthDelta,
        bool tailWillLeave,
        List<GridPosition> selfCollisionCells,
        List<GridPosition> resultingBodyCells)
    {
        NextHead = nextHead ?? throw new ArgumentNullException(nameof(nextHead));
        if (growthDelta < 0 || growthDelta > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(growthDelta));
        }

        ArgumentNullException.ThrowIfNull(selfCollisionCells);
        ArgumentNullException.ThrowIfNull(resultingBodyCells);
        GrowthDelta = growthDelta;
        TailWillLeave = tailWillLeave;
        _selfCollisionCells = new List<GridPosition>(selfCollisionCells);
        _resultingBodyCells = new List<GridPosition>(resultingBodyCells);
    }

    public GridPosition NextHead { get; }
    public int GrowthDelta { get; }
    public bool TailWillLeave { get; }
    public List<GridPosition> SelfCollisionCells => new List<GridPosition>(_selfCollisionCells);
    public List<GridPosition> ResultingBodyCells => new List<GridPosition>(_resultingBodyCells);
}
}
