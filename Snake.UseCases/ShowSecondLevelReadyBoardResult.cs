namespace Snake.UseCases;

public class ShowSecondLevelReadyBoardResult
{
    public ShowSecondLevelReadyBoardResult(
        bool isSuccess,
        string? failureCode,
        string? failureMessage,
        SecondLevelReadyBoardRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public SecondLevelReadyBoardRspDTO? Data { get; }
}
