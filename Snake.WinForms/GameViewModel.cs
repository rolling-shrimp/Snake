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

    public string ScreenId { get; }
    public GameBoardViewModel Board { get; }
    public GameStatusViewModel? Status { get; }
    public List<RuleHintViewModel> RuleHints => new List<RuleHintViewModel>(_ruleHints);
    public bool IsStartButtonVisible { get; }
    public bool IsReplayButtonVisible { get; }
    public bool IsMovementActive { get; }
    public int MovementIntervalMilliseconds { get; }
    public GameResultCardViewModel? ResultCard { get; }
}
