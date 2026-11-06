using Snake.UseCases;

namespace Snake.WinForms;

public class BoardObjectViewModel
{
    public BoardObjectViewModel(BoardObjectKind type, string itemId, GridCellViewModel position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        Type = type;
        ItemId = itemId;
        Position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public BoardObjectKind Type { get; }
    public string ItemId { get; }
    public GridCellViewModel Position { get; }
}
