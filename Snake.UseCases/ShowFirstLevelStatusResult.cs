namespace Snake.UseCases;

public class ShowFirstLevelStatusResult
{
    public ShowFirstLevelStatusResult(bool isSuccess, string? failureCode, string? failureMessage, FirstLevelStatusRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public FirstLevelStatusRspDTO? Data { get; }
}
