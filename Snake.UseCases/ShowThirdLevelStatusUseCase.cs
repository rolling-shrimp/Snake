namespace Snake.UseCases;

public class ShowThirdLevelStatusUseCase
{
    private IThirdLevelStatusReadPort _readPort;

    public ShowThirdLevelStatusUseCase(IThirdLevelStatusReadPort readPort)
    {
        _readPort = readPort;
    }

    public ShowThirdLevelStatusResult Execute(ShowThirdLevelStatusQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!_readPort.IsAvailable)
        {
            return new ShowThirdLevelStatusResult(
                false,
                "INVALID_GAME_PHASE",
                "目前不是第三關待開始階段。",
                null);
        }

        return new ShowThirdLevelStatusResult(true, null, null, _readPort.Read());
    }
}
