namespace Snake.UseCases;

public class DoorStatusDTO
{
    public DoorStatusDTO(Guid doorId, GridPositionDTO position, string state)
    {
        DoorId = doorId;
        Position = position;
        State = state;
    }

    public Guid DoorId { get; }
    public GridPositionDTO Position { get; }
    public string State { get; }
}
