namespace Snake.UseCases;

public class ShowSecondLevelRulesResult
{
    public ShowSecondLevelRulesResult(
        bool isSuccess,
        string? failureCode,
        string? failureMessage,
        SecondLevelRulesRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public SecondLevelRulesRspDTO? Data { get; }
}
