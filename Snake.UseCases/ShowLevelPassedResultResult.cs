namespace Snake.UseCases;

public class ShowLevelPassedResultResult
{
    public ShowLevelPassedResultResult(
        bool isSuccess,
        string? failureCode,
        string? failureMessage,
        LevelPassedResultRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public LevelPassedResultRspDTO? Data { get; }
}
