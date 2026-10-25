namespace Snake.UseCases;

public class ShowFirstLevelStatusUseCase
{
    private IFirstLevelReadPort _readPort;
    private FirstLevelStatusProjection _projection;

    public ShowFirstLevelStatusUseCase(IFirstLevelReadPort readPort, FirstLevelStatusProjection projection)
    {
        _readPort = readPort;
        _projection = projection;
    }

    public ShowFirstLevelStatusResult Execute(ShowFirstLevelStatusQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        FirstLevelReadModel readModel = _readPort.Read();
        if (!readModel.IsReady || readModel.LevelNumber != 1)
        {
            return new ShowFirstLevelStatusResult(false, "INVALID_GAME_PHASE", "目前不是第一關待開始階段。", null);
        }

        return new ShowFirstLevelStatusResult(true, null, null, _projection.Project(readModel));
    }
}
