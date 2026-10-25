namespace Snake.UseCases;

public class ShowThirdLevelReadyBoardResult
{
    public ShowThirdLevelReadyBoardResult(
        bool isSuccess,
        string? failureCode,
        string? failureMessage,
        ThirdLevelReadyBoardRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public ThirdLevelReadyBoardRspDTO? Data { get; }
}
