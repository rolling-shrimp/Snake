namespace Snake.UseCases;

public class ShowFirstLevelReadyBoardResult
{
    public ShowFirstLevelReadyBoardResult(bool isSuccess, string? failureCode, string? failureMessage, FirstLevelReadyBoardRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public FirstLevelReadyBoardRspDTO? Data { get; }
}
