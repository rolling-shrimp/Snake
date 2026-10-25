namespace Snake.UseCases;

public class HandleSelfCollisionCommand
{
    public HandleSelfCollisionCommand(GridPositionDTO collisionPosition)
    {
        CollisionPosition = collisionPosition ?? throw new ArgumentNullException(nameof(collisionPosition));
    }

    public GridPositionDTO CollisionPosition { get; }
}
