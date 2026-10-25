namespace Snake.WinForms;

public class GridCellViewModel
{
    public GridCellViewModel(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }
}
