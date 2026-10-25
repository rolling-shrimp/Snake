namespace Snake.WinForms;

public class GameViewModel
{
    private List<RuleHintViewModel> _ruleHints;

    public GameViewModel(
        string screenId,
        GameBoardViewModel board,
        GameStatusViewModel? status,
        List<RuleHintViewModel> ruleHints,
        bool isStartButtonVisible,
        bool isReplayButtonVisible,
        bool isMovementActive,
        int movementIntervalMilliseconds,
        GameResultCardViewModel? resultCard)
    {
        ScreenId = screenId;
        Board = board ?? throw new ArgumentNullException(nameof(board));
        Status = status;
        _ruleHints = new List<RuleHintViewModel>(ruleHints);
        IsStartButtonVisible = isStartButtonVisible;
        IsReplayButtonVisible = isReplayButtonVisible;
        IsMovementActive = isMovementActive;
        MovementIntervalMilliseconds = movementIntervalMilliseconds;
        ResultCard = resultCard;
    }

    public GameViewModel(
        int boardRows,
        int boardColumns,
        List<GridCellViewModel> snakeCells,
        List<RuleHintViewModel> ruleHints,
        string levelName,
        int currentLength,
        int targetLength,
        int progressPercent,
        bool isStartButtonVisible,
        GridCellViewModel? strawberryCell,
        bool isMovementActive,
        int movementIntervalMilliseconds)
    {
        ScreenId = isMovementActive ? "first-level-playing" : "first-level-ready";
        List<BoardObjectViewModel> objects = strawberryCell is null
            ? new List<BoardObjectViewModel>()
            : new List<BoardObjectViewModel>
            {
                new BoardObjectViewModel(
                    Snake.UseCases.GameObjectType.Strawberry,
                    "legacy-strawberry",
                    strawberryCell)
            };
        Board = new GameBoardViewModel(
            true,
            boardRows,
            boardColumns,
            snakeCells,
            new List<GridCellViewModel>(),
            new List<DoorViewModel>(),
            objects);
        Status = new GameStatusViewModel(
            levelName,
            currentLength,
            targetLength,
            progressPercent,
            null);
        _ruleHints = new List<RuleHintViewModel>(ruleHints);
        IsStartButtonVisible = isStartButtonVisible;
        IsReplayButtonVisible = false;
        IsMovementActive = isMovementActive;
        MovementIntervalMilliseconds = movementIntervalMilliseconds;
        ResultCard = null;
    }

    public string ScreenId { get; }
    public GameBoardViewModel Board { get; }
    public GameStatusViewModel? Status { get; }
    public List<RuleHintViewModel> RuleHints => new List<RuleHintViewModel>(_ruleHints);
    public bool IsStartButtonVisible { get; }
    public bool IsReplayButtonVisible { get; }
    public bool IsMovementActive { get; }
    public int MovementIntervalMilliseconds { get; }
    public GameResultCardViewModel? ResultCard { get; }
    public int BoardRows => Board.Rows;
    public int BoardColumns => Board.Columns;
    public List<GridCellViewModel> SnakeCells => Board.SnakeCells;
    public string LevelName => Status?.LevelName ?? string.Empty;
    public int CurrentLength => Status?.CurrentLength ?? 0;
    public int TargetLength => Status?.TargetLength ?? 0;
    public int ProgressPercent => Status?.ProgressPercent ?? 0;
    public GridCellViewModel? StrawberryCell => Board.Objects
        .FirstOrDefault(gameObject => gameObject.Type == Snake.UseCases.GameObjectType.Strawberry)
        ?.Position;
}
