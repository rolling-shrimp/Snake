namespace Snake.UseCases;

public class ProcessGameTickResult
{
    private List<GameTickEventDTO> _processedEvents;

    public ProcessGameTickResult(
        bool isSuccess,
        ProcessGameTickOutcome outcome,
        string? failureCode,
        string? failureMessage,
        GameScreenOutput screen,
        RuntimeDirective runtimeDirective,
        List<GameTickEventDTO> processedEvents)
    {
        IsSuccess = isSuccess;
        Outcome = outcome;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Screen = screen ?? throw new ArgumentNullException(nameof(screen));
        RuntimeDirective = runtimeDirective;
        ArgumentNullException.ThrowIfNull(processedEvents);
        _processedEvents = new List<GameTickEventDTO>(processedEvents);
    }

    public bool IsSuccess { get; }
    public ProcessGameTickOutcome Outcome { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public GameScreenOutput Screen { get; }
    public RuntimeDirective RuntimeDirective { get; }
    public List<GameTickEventDTO> ProcessedEvents => new List<GameTickEventDTO>(_processedEvents);
}
