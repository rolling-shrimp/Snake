namespace Snake.UseCases;

public class ShowLevelPassedResultUseCase
{
    private ILevelPassedResultReadPort _readPort;

    public ShowLevelPassedResultUseCase(ILevelPassedResultReadPort readPort)
    {
        _readPort = readPort;
    }

    public ShowLevelPassedResultResult Execute(ShowLevelPassedResultQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!_readPort.IsAvailable)
        {
            return new ShowLevelPassedResultResult(
                false,
                "RESULT_NOT_AVAILABLE",
                "目前沒有可顯示的關卡通過結果。",
                null);
        }

        return new ShowLevelPassedResultResult(true, null, null, _readPort.Read());
    }
}
