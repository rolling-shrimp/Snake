namespace Snake.Entities;

public class BombCollisionDomainEvent : IDomainEvent
{
    public BombCollisionDomainEvent(BombItemId bombItemId)
    {
        BombItemId = bombItemId ?? throw new ArgumentNullException(nameof(bombItemId));
    }

    public BombItemId BombItemId { get; }
}
