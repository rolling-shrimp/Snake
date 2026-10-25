namespace Snake.UseCases;

public class ShowGameOverResultResult
{
    public ShowGameOverResultResult(
        bool isSuccess,
        string? failureCode,
        string? failureMessage,
        GameOverResultRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public GameOverResultRspDTO? Data { get; }
}
