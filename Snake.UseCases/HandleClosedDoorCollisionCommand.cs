namespace Snake.UseCases;

public class HandleClosedDoorCollisionCommand
{
    public HandleClosedDoorCollisionCommand(string doorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(doorId);
        DoorId = doorId;
    }

    public string DoorId { get; }
}
