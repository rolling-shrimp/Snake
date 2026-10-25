namespace Snake.UseCases;

public class GameplayCollisionDTO
{
    public GameplayCollisionDTO(
        GameplayCollisionType type,
        GridPositionDTO position,
        string? doorId)
    {
        Type = type;
        Position = position;
        DoorId = doorId;
    }

    public GameplayCollisionType Type { get; }
    public GridPositionDTO Position { get; }
    public string? DoorId { get; }
}
