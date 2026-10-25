namespace Snake.UseCases;

public class GameObjectStateDTO
{
    public GameObjectStateDTO(
        GameObjectType type,
        string itemId,
        GridPositionDTO position)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        Type = type;
        ItemId = itemId;
        Position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public GameObjectType Type { get; }
    public string ItemId { get; }
    public GridPositionDTO Position { get; }
}
