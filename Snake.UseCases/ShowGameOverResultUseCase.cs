namespace Snake.UseCases;

public class ShowGameOverResultUseCase
{
    private IGameOverResultReadPort _readPort;

    public ShowGameOverResultUseCase(IGameOverResultReadPort readPort)
    {
        _readPort = readPort;
    }

    public ShowGameOverResultResult Execute(ShowGameOverResultQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!_readPort.IsAvailable)
        {
            return new ShowGameOverResultResult(
                false,
                "RESULT_NOT_AVAILABLE",
                "目前沒有可顯示的失敗結果。",
                null);
        }

        return new ShowGameOverResultResult(true, null, null, _readPort.Read());
    }
}
