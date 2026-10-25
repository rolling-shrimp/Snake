namespace Snake.UseCases;

public class HandleShieldContactResult
{
    public HandleShieldContactResult(bool isSuccess, string? failureCode, string? failureMessage)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
}
