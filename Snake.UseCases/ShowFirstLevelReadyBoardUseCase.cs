namespace Snake.UseCases;

public class ShowFirstLevelReadyBoardUseCase
{
    private IFirstLevelReadPort _readPort;
    private FirstLevelReadyBoardProjection _projection;

    public ShowFirstLevelReadyBoardUseCase(IFirstLevelReadPort readPort, FirstLevelReadyBoardProjection projection)
    {
        _readPort = readPort;
        _projection = projection;
    }

    public ShowFirstLevelReadyBoardResult Execute(ShowFirstLevelReadyBoardQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        FirstLevelReadModel readModel = _readPort.Read();
        if (!readModel.IsReady || readModel.LevelNumber != 1)
        {
            return new ShowFirstLevelReadyBoardResult(false, "INVALID_GAME_PHASE", "目前不是第一關待開始階段。", null);
        }

        return new ShowFirstLevelReadyBoardResult(true, null, null, _projection.Project(readModel));
    }
}
