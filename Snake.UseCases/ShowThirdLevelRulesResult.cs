namespace Snake.UseCases;

public class ShowThirdLevelRulesResult
{
    public ShowThirdLevelRulesResult(
        bool isSuccess,
        string? failureCode,
        string? failureMessage,
        ThirdLevelRulesRspDTO? data)
    {
        IsSuccess = isSuccess;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Data = data;
    }

    public bool IsSuccess { get; }
    public string? FailureCode { get; }
    public string? FailureMessage { get; }
    public ThirdLevelRulesRspDTO? Data { get; }
}
