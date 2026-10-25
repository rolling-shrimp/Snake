namespace Snake.UseCases;

public class SecondLevelReadyBoardRspDTO
{
    private List<GridPositionDTO> _snakeCells;

    public SecondLevelReadyBoardRspDTO(
        int boardRows,
        int boardColumns,
        List<GridPositionDTO> snakeCells,
        bool isStartButtonVisible)
    {
        BoardRows = boardRows;
        BoardColumns = boardColumns;
        _snakeCells = new List<GridPositionDTO>(snakeCells);
        IsStartButtonVisible = isStartButtonVisible;
    }

    public int BoardRows { get; }
    public int BoardColumns { get; }
    public List<GridPositionDTO> SnakeCells => new List<GridPositionDTO>(_snakeCells);
    public bool IsStartButtonVisible { get; }
}
