namespace Snake.Entities;

public class ClosedDoorCollisionDomainEvent : IDomainEvent
{
    public ClosedDoorCollisionDomainEvent(DoorId doorId)
    {
        DoorId = doorId ?? throw new ArgumentNullException(nameof(doorId));
    }

    public DoorId DoorId { get; }
}
