namespace Snake.Entities;

public class SnakeMovementPlanResult
{
    public SnakeMovementPlanResult(
        bool isSuccess,
        SnakeMovementPlan? plan,
        string? failureCode,
        string? failureMessage)
    {
        IsSuccess = isSuccess;
        Plan = plan;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }

    public bool IsSuccess { get; }
    public SnakeMovementPlan? Plan { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
}
