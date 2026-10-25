namespace Snake.Entities;

public class SelfCollisionDomainEvent : IDomainEvent
{
    public SelfCollisionDomainEvent(GridPosition collisionPosition)
    {
        CollisionPosition = collisionPosition ?? throw new ArgumentNullException(nameof(collisionPosition));
    }

    public GridPosition CollisionPosition { get; }
}
