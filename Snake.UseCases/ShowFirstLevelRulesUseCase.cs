namespace Snake.UseCases;

public class ShowFirstLevelRulesUseCase
{
    private IFirstLevelReadPort _readPort;
    private FirstLevelRulesProjection _projection;

    public ShowFirstLevelRulesUseCase(IFirstLevelReadPort readPort, FirstLevelRulesProjection projection)
    {
        _readPort = readPort;
        _projection = projection;
    }

    public ShowFirstLevelRulesResult Execute(ShowFirstLevelRulesQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        FirstLevelReadModel readModel = _readPort.Read();
        if (!readModel.IsReady || readModel.LevelNumber != 1)
        {
            return new ShowFirstLevelRulesResult(false, "INVALID_GAME_PHASE", "目前不是第一關待開始階段。", null);
        }

        return new ShowFirstLevelRulesResult(true, null, null, _projection.Project(readModel));
    }
}
