namespace Snake.UseCases;

public class GameplayReadModel
{
    private List<GridPositionDTO> _snakeCells;
    private List<GameObjectStateDTO> _objects;
    private List<GridPositionDTO> _wallCells;
    private List<DoorStatusDTO> _doors;

    public GameplayReadModel(
        GameplayPhase phase,
        int levelNumber,
        int boardRows,
        int boardColumns,
        List<GridPositionDTO> snakeCells,
        List<GameObjectStateDTO> objects,
        List<GridPositionDTO> wallCells,
        List<DoorStatusDTO> doors,
        int currentLength,
        int targetLength,
        int progressPercent,
        int shieldRemainingCharges,
        bool isMovementActive,
        int movementIntervalMilliseconds,
        int doorToggleIntervalMilliseconds,
        int doorWarningMilliseconds,
        GameplayCollisionDTO? lastMovementCollision)
    {
        Phase = phase;
        LevelNumber = levelNumber;
        BoardRows = boardRows;
        BoardColumns = boardColumns;
        _snakeCells = new List<GridPositionDTO>(snakeCells);
        _objects = new List<GameObjectStateDTO>(objects);
        _wallCells = new List<GridPositionDTO>(wallCells);
        _doors = new List<DoorStatusDTO>(doors);
        CurrentLength = currentLength;
        TargetLength = targetLength;
        ProgressPercent = progressPercent;
        ShieldRemainingCharges = shieldRemainingCharges;
        IsMovementActive = isMovementActive;
        MovementIntervalMilliseconds = movementIntervalMilliseconds;
        DoorToggleIntervalMilliseconds = doorToggleIntervalMilliseconds;
        DoorWarningMilliseconds = doorWarningMilliseconds;
        LastMovementCollision = lastMovementCollision;
    }

    public GameplayPhase Phase { get; }
    public int LevelNumber { get; }
    public int BoardRows { get; }
    public int BoardColumns { get; }
    public List<GridPositionDTO> SnakeCells => new List<GridPositionDTO>(_snakeCells);
    public List<GameObjectStateDTO> Objects => new List<GameObjectStateDTO>(_objects);
    public List<GridPositionDTO> WallCells => new List<GridPositionDTO>(_wallCells);
    public List<DoorStatusDTO> Doors => new List<DoorStatusDTO>(_doors);
    public int CurrentLength { get; }
    public int TargetLength { get; }
    public int ProgressPercent { get; }
    public int ShieldRemainingCharges { get; }
    public bool IsMovementActive { get; }
    public int MovementIntervalMilliseconds { get; }
    public int DoorToggleIntervalMilliseconds { get; }
    public int DoorWarningMilliseconds { get; }
    public GameplayCollisionDTO? LastMovementCollision { get; }
}
