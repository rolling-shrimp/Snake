namespace Snake.UseCases;

public class FirstLevelReadyBoardProjection
{
    public FirstLevelReadyBoardRspDTO Project(FirstLevelReadModel readModel)
    {
        return new FirstLevelReadyBoardRspDTO(
            readModel.BoardRows,
            readModel.BoardColumns,
            readModel.SnakeCells,
            readModel.CanStart);
    }
}
