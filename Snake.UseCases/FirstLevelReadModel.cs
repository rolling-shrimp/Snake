namespace Snake.UseCases;

public class FirstLevelReadModel
{
    private List<GridPositionDTO> _snakeCells;

    public FirstLevelReadModel(
        bool isReady,
        int levelNumber,
        int boardRows,
        int boardColumns,
        List<GridPositionDTO> snakeCells,
        bool canStart,
        int ordinaryFoodLengthDelta,
        bool boundaryWraps,
        bool selfCollisionIsFatal,
        int targetLength,
        int progressPercent)
    {
        IsReady = isReady;
        LevelNumber = levelNumber;
        BoardRows = boardRows;
        BoardColumns = boardColumns;
        _snakeCells = new List<GridPositionDTO>(snakeCells);
        CanStart = canStart;
        OrdinaryFoodLengthDelta = ordinaryFoodLengthDelta;
        BoundaryWraps = boundaryWraps;
        SelfCollisionIsFatal = selfCollisionIsFatal;
        TargetLength = targetLength;
        ProgressPercent = progressPercent;
    }

    public bool IsReady { get; }
    public int LevelNumber { get; }
    public int BoardRows { get; }
    public int BoardColumns { get; }
    public List<GridPositionDTO> SnakeCells => new List<GridPositionDTO>(_snakeCells);
    public bool CanStart { get; }
    public int OrdinaryFoodLengthDelta { get; }
    public bool BoundaryWraps { get; }
    public bool SelfCollisionIsFatal { get; }
    public int CurrentLength => _snakeCells.Count;
    public int TargetLength { get; }
    public int ProgressPercent { get; }
}
