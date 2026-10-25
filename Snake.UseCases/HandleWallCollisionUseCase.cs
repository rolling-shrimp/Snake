using Snake.Entities;

namespace Snake.UseCases;

public class HandleWallCollisionUseCase
{
    private SnakeGameSession _session;
    private HashSet<string> _processedPositions;

    public HandleWallCollisionUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _processedPositions = new HashSet<string>(StringComparer.Ordinal);
    }

    public HandleWallCollisionResult Execute(HandleWallCollisionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_session.Game.MovementActive || _session.Level.Number != LevelNumber.Third)
        {
            return Succeed();
        }

        string collisionKey = $"{command.CollisionPosition.X}:{command.CollisionPosition.Y}";
        if (_processedPositions.Contains(collisionKey))
        {
            return Succeed();
        }

        _session.PublishFact(new WallCollisionDomainEvent(
            command.CollisionPosition.X,
            command.CollisionPosition.Y));
        _processedPositions.Add(collisionKey);
        return Succeed();
    }

    private HandleWallCollisionResult Succeed()
    {
        return new HandleWallCollisionResult(true, null, null);
    }
}
