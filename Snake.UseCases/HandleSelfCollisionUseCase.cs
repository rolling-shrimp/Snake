using Snake.Entities;

namespace Snake.UseCases;

public class HandleSelfCollisionUseCase
{
    private SnakeGameSession _session;
    private HashSet<GridPosition> _processedPositions;

    public HandleSelfCollisionUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _processedPositions = new HashSet<GridPosition>();
    }

    public HandleSelfCollisionResult Execute(HandleSelfCollisionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_session.Game.MovementActive)
        {
            return Succeed();
        }

        GridPosition collisionPosition = new GridPosition(
            command.CollisionPosition.X,
            command.CollisionPosition.Y);
        if (!_session.Snake.BodyCells.Contains(collisionPosition)
            || _processedPositions.Contains(collisionPosition))
        {
            return Succeed();
        }

        _session.PublishFact(new SelfCollisionDomainEvent(collisionPosition));
        _processedPositions.Add(collisionPosition);
        return Succeed();
    }

    private HandleSelfCollisionResult Succeed()
    {
        return new HandleSelfCollisionResult(true, null, null);
    }
}
