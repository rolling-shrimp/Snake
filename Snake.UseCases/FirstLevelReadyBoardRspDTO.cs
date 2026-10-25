namespace Snake.UseCases;

public class FirstLevelReadyBoardRspDTO
{
    private List<GridPositionDTO> _snakeCells;

    public FirstLevelReadyBoardRspDTO(int boardRows, int boardColumns, List<GridPositionDTO> snakeCells, bool isStartButtonVisible)
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
