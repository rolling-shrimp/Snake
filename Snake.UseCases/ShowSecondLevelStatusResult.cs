namespace Snake.UseCases;

public class ShowSecondLevelStatusResult
{
    public ShowSecondLevelStatusResult(
        bool isSuccess,
        string? failureCode,
        string? failureMessage,
        SecondLevelStatusRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public SecondLevelStatusRspDTO? Data { get; }
}
