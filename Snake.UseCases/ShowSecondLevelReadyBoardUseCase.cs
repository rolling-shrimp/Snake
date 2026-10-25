namespace Snake.UseCases;

public class ShowSecondLevelReadyBoardUseCase
{
    private ISecondLevelReadyBoardReadPort _readPort;

    public ShowSecondLevelReadyBoardUseCase(ISecondLevelReadyBoardReadPort readPort)
    {
        _readPort = readPort;
    }

    public ShowSecondLevelReadyBoardResult Execute(ShowSecondLevelReadyBoardQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!_readPort.IsAvailable)
        {
            return new ShowSecondLevelReadyBoardResult(
                false,
                "INVALID_GAME_PHASE",
                "目前不是第二關待開始階段。",
                null);
        }

        return new ShowSecondLevelReadyBoardResult(true, null, null, _readPort.Read());
    }
}
