namespace Snake.Entities;

public class WallCollisionDomainEvent : IDomainEvent
{
    public WallCollisionDomainEvent(int attemptedX, int attemptedY)
    {
        AttemptedX = attemptedX;
        AttemptedY = attemptedY;
    }

    public int AttemptedX { get; }
    public int AttemptedY { get; }
}
