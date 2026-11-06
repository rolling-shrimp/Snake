namespace Snake.UseCases;

public class BoardObjectOutput
{
    public BoardObjectOutput(string objectId, BoardObjectKind kind, GridPositionDTO position)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            throw new ArgumentException("物件識別碼不得為空。", nameof(objectId));
        }

        ObjectId = objectId;
        Kind = kind;
        Position = position ?? throw new ArgumentNullException(nameof(position));
    }

    public string ObjectId { get; }
    public BoardObjectKind Kind { get; }
    public GridPositionDTO Position { get; }
}
