namespace Snake.UseCases;

public class ShowGameCompletedResultUseCase
{
    private IGameCompletedResultReadPort _readPort;

    public ShowGameCompletedResultUseCase(IGameCompletedResultReadPort readPort)
    {
        _readPort = readPort;
    }

    public ShowGameCompletedResultResult Execute(ShowGameCompletedResultQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!_readPort.IsAvailable)
        {
            return new ShowGameCompletedResultResult(
                false,
                "RESULT_NOT_AVAILABLE",
                "目前沒有可顯示的整輪完成結果。",
                null);
        }

        return new ShowGameCompletedResultResult(true, null, null, _readPort.Read());
    }
}
