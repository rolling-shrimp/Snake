namespace Snake.UseCases;

public class ShowThirdLevelRulesUseCase
{
    private IThirdLevelRulesReadPort _readPort;

    public ShowThirdLevelRulesUseCase(IThirdLevelRulesReadPort readPort)
    {
        _readPort = readPort;
    }

    public ShowThirdLevelRulesResult Execute(ShowThirdLevelRulesQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!_readPort.IsAvailable)
        {
            return new ShowThirdLevelRulesResult(
                false,
                "INVALID_GAME_PHASE",
                "目前不是第三關待開始階段。",
                null);
        }

        return new ShowThirdLevelRulesResult(true, null, null, _readPort.Read());
    }
}
