namespace Snake.UseCases;

public class GameScreenOutput
{
    private List<BoardCellOutput> _boardCells;
    private List<GridPositionDTO> _snakeBody;
    private List<BoardObjectOutput> _objects;
    private List<GridPositionDTO> _wallCells;
    private List<DoorOutput> _doors;
    private List<string> _ruleHints;
    private List<GameAction> _availableActions;

    public GameScreenOutput(
        GameScreenKind screenKind,
        List<BoardCellOutput> boardCells,
        List<GridPositionDTO> snakeBody,
        List<BoardObjectOutput> objects,
        List<GridPositionDTO> wallCells,
        List<DoorOutput> doors,
        LevelStatusOutput levelStatus,
        List<string> ruleHints,
        ResultCardOutput? resultCard,
        List<GameAction> availableActions)
    {
        ArgumentNullException.ThrowIfNull(boardCells);
        ArgumentNullException.ThrowIfNull(snakeBody);
        ArgumentNullException.ThrowIfNull(objects);
        ArgumentNullException.ThrowIfNull(wallCells);
        ArgumentNullException.ThrowIfNull(doors);
        ArgumentNullException.ThrowIfNull(ruleHints);
        ArgumentNullException.ThrowIfNull(availableActions);
        ScreenKind = screenKind;
        _boardCells = new List<BoardCellOutput>(boardCells);
        _snakeBody = new List<GridPositionDTO>(snakeBody);
        _objects = new List<BoardObjectOutput>(objects);
        _wallCells = new List<GridPositionDTO>(wallCells);
        _doors = new List<DoorOutput>(doors);
        LevelStatus = levelStatus ?? throw new ArgumentNullException(nameof(levelStatus));
        _ruleHints = new List<string>(ruleHints);
        ResultCard = resultCard;
        _availableActions = new List<GameAction>(availableActions);
    }

    public GameScreenKind ScreenKind { get; }
    public List<BoardCellOutput> BoardCells => new List<BoardCellOutput>(_boardCells);
    public List<GridPositionDTO> SnakeBody => new List<GridPositionDTO>(_snakeBody);
    public List<BoardObjectOutput> Objects => new List<BoardObjectOutput>(_objects);
    public List<GridPositionDTO> WallCells => new List<GridPositionDTO>(_wallCells);
    public List<DoorOutput> Doors => new List<DoorOutput>(_doors);
    public LevelStatusOutput LevelStatus { get; }
    public List<string> RuleHints => new List<string>(_ruleHints);
    public ResultCardOutput? ResultCard { get; }
    public List<GameAction> AvailableActions => new List<GameAction>(_availableActions);
}
