namespace Snake.UseCases;

public class ShowThirdLevelStatusResult
{
    public ShowThirdLevelStatusResult(
        bool isSuccess,
        string? failureCode,
        string? failureMessage,
        ThirdLevelStatusRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public ThirdLevelStatusRspDTO? Data { get; }
}
