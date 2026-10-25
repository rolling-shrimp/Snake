namespace Snake.UseCases;

public class ShowThirdLevelReadyBoardUseCase
{
    private IThirdLevelReadyBoardReadPort _readPort;

    public ShowThirdLevelReadyBoardUseCase(IThirdLevelReadyBoardReadPort readPort)
    {
        _readPort = readPort;
    }

    public ShowThirdLevelReadyBoardResult Execute(ShowThirdLevelReadyBoardQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!_readPort.IsAvailable)
        {
            return new ShowThirdLevelReadyBoardResult(
                false,
                "INVALID_GAME_PHASE",
                "目前不是第三關待開始階段。",
                null);
        }

        return new ShowThirdLevelReadyBoardResult(true, null, null, _readPort.Read());
    }
}
