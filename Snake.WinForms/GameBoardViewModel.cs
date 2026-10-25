namespace Snake.WinForms;

public class GameBoardViewModel
{
    private List<GridCellViewModel> _snakeCells;
    private List<GridCellViewModel> _wallCells;
    private List<DoorViewModel> _doors;
    private List<BoardObjectViewModel> _objects;

    public GameBoardViewModel(
        bool isVisible,
        int rows,
        int columns,
        List<GridCellViewModel> snakeCells,
        List<GridCellViewModel> wallCells,
        List<DoorViewModel> doors,
        List<BoardObjectViewModel> objects)
    {
        IsVisible = isVisible;
        Rows = rows;
        Columns = columns;
        _snakeCells = new List<GridCellViewModel>(snakeCells);
        _wallCells = new List<GridCellViewModel>(wallCells);
        _doors = new List<DoorViewModel>(doors);
        _objects = new List<BoardObjectViewModel>(objects);
    }

    public bool IsVisible { get; }
    public int Rows { get; }
    public int Columns { get; }
    public List<GridCellViewModel> SnakeCells => new List<GridCellViewModel>(_snakeCells);
    public List<GridCellViewModel> WallCells => new List<GridCellViewModel>(_wallCells);
    public List<DoorViewModel> Doors => new List<DoorViewModel>(_doors);
    public List<BoardObjectViewModel> Objects => new List<BoardObjectViewModel>(_objects);
}
