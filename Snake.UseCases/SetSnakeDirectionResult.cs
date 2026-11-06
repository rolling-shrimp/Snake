namespace Snake.UseCases;

public class SetSnakeDirectionResult
{
    public SetSnakeDirectionResult(
        bool isSuccess,
        SetSnakeDirectionOutcome outcome,
        string? failureCode,
        string? failureMessage,
        GameScreenOutput? screen,
        RuntimeDirective runtimeDirective)
    {
        IsSuccess = isSuccess;
        Outcome = outcome;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Screen = screen;
        RuntimeDirective = runtimeDirective;
    }

    public bool IsSuccess { get; }
    public SetSnakeDirectionOutcome Outcome { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public GameScreenOutput? Screen { get; }
    public RuntimeDirective RuntimeDirective { get; }
}
