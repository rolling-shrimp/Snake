namespace Snake.UseCases;

public class ShowSecondLevelStatusUseCase
{
    private ISecondLevelStatusReadPort _readPort;

    public ShowSecondLevelStatusUseCase(ISecondLevelStatusReadPort readPort)
    {
        _readPort = readPort;
    }

    public ShowSecondLevelStatusResult Execute(ShowSecondLevelStatusQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!_readPort.IsAvailable)
        {
            return new ShowSecondLevelStatusResult(
                false,
                "INVALID_GAME_PHASE",
                "目前不是第二關待開始階段。",
                null);
        }

        return new ShowSecondLevelStatusResult(true, null, null, _readPort.Read());
    }
}
