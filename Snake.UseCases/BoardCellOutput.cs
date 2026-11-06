namespace Snake.UseCases;

public class BoardCellOutput
{
    public BoardCellOutput(GridPositionDTO position, BoardCellKind kind)
    {
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Kind = kind;
    }

    public GridPositionDTO Position { get; }
    public BoardCellKind Kind { get; }
}
