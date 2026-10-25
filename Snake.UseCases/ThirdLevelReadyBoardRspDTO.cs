namespace Snake.UseCases;

public class ThirdLevelReadyBoardRspDTO
{
    private List<GridPositionDTO> _snakeCells;
    private List<GridPositionDTO> _wallCells;
    private List<DoorStatusDTO> _doors;

    public ThirdLevelReadyBoardRspDTO(
        int boardRows,
        int boardColumns,
        List<GridPositionDTO> snakeCells,
        List<GridPositionDTO> wallCells,
        List<DoorStatusDTO> doors,
        bool isStartButtonVisible)
    {
        BoardRows = boardRows;
        BoardColumns = boardColumns;
        _snakeCells = new List<GridPositionDTO>(snakeCells);
        _wallCells = new List<GridPositionDTO>(wallCells);
        _doors = new List<DoorStatusDTO>(doors);
        IsStartButtonVisible = isStartButtonVisible;
    }

    public int BoardRows { get; }
    public int BoardColumns { get; }
    public List<GridPositionDTO> SnakeCells => new List<GridPositionDTO>(_snakeCells);
    public List<GridPositionDTO> WallCells => new List<GridPositionDTO>(_wallCells);
    public List<DoorStatusDTO> Doors => new List<DoorStatusDTO>(_doors);
    public bool IsStartButtonVisible { get; }
}
