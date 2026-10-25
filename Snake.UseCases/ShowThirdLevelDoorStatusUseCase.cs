namespace Snake.UseCases;

public class ShowThirdLevelDoorStatusUseCase
{
    private IThirdLevelDoorStatusReadPort _readPort;

    public ShowThirdLevelDoorStatusUseCase(IThirdLevelDoorStatusReadPort readPort)
    {
        _readPort = readPort;
    }

    public ShowThirdLevelDoorStatusResult Execute(ShowThirdLevelDoorStatusQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!_readPort.IsAvailable)
        {
            return new ShowThirdLevelDoorStatusResult(
                false,
                "INVALID_GAME_PHASE",
                "目前沒有可顯示的第三關門狀態。",
                null);
        }

        return new ShowThirdLevelDoorStatusResult(true, null, null, _readPort.Read());
    }
}
