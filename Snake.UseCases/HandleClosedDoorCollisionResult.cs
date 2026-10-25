namespace Snake.UseCases;

public class HandleClosedDoorCollisionResult
{
    public HandleClosedDoorCollisionResult(bool isSuccess, string? failureCode, string? failureMessage)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
}
