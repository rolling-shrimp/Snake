namespace Snake.UseCases;

public class HandleWallCollisionCommand
{
    public HandleWallCollisionCommand(GridPositionDTO collisionPosition)
    {
        CollisionPosition = collisionPosition ?? throw new ArgumentNullException(nameof(collisionPosition));
    }

    public GridPositionDTO CollisionPosition { get; }
}
