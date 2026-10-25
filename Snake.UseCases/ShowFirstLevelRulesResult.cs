namespace Snake.UseCases;

public class ShowFirstLevelRulesResult
{
    public ShowFirstLevelRulesResult(bool isSuccess, string? failureCode, string? failureMessage, FirstLevelRulesRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public FirstLevelRulesRspDTO? Data { get; }
}
