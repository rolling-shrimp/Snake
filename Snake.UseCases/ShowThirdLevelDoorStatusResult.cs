namespace Snake.UseCases;

public class ShowThirdLevelDoorStatusResult
{
    public ShowThirdLevelDoorStatusResult(
        bool isSuccess,
        string? failureCode,
        string? failureMessage,
        ThirdLevelDoorStatusRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public ThirdLevelDoorStatusRspDTO? Data { get; }
}
