namespace Snake.UseCases;

public class ShowGameCompletedResultResult
{
    public ShowGameCompletedResultResult(
        bool isSuccess,
        string? failureCode,
        string? failureMessage,
        GameCompletedResultRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public GameCompletedResultRspDTO? Data { get; }
}
