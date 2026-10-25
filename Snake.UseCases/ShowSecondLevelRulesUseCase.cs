namespace Snake.UseCases;

public class ShowSecondLevelRulesUseCase
{
    private ISecondLevelRulesReadPort _readPort;

    public ShowSecondLevelRulesUseCase(ISecondLevelRulesReadPort readPort)
    {
        _readPort = readPort;
    }

    public ShowSecondLevelRulesResult Execute(ShowSecondLevelRulesQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!_readPort.IsAvailable)
        {
            return new ShowSecondLevelRulesResult(
                false,
                "INVALID_GAME_PHASE",
                "目前不是第二關待開始階段。",
                null);
        }

        return new ShowSecondLevelRulesResult(true, null, null, _readPort.Read());
    }
}
