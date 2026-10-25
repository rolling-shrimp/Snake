using Snake.Entities;

namespace Snake.UseCases;

public class HandleClosedDoorCollisionUseCase
{
    private SnakeGameSession _session;
    private HashSet<Guid> _processedDoorIds;

    public HandleClosedDoorCollisionUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _processedDoorIds = new HashSet<Guid>();
    }

    public HandleClosedDoorCollisionResult Execute(HandleClosedDoorCollisionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_session.Game.MovementActive
            || _session.Level.Number != LevelNumber.Third
            || !Guid.TryParse(command.DoorId, out Guid doorIdValue)
            || _processedDoorIds.Contains(doorIdValue))
        {
            return Succeed();
        }

        DoorId doorId = new DoorId(doorIdValue);
        Door? door = _session.DoorAndMaze.FindDoor(doorId);
        if (door is null || door.Phase != DoorPhase.Closed)
        {
            return Succeed();
        }

        _session.PublishFact(new ClosedDoorCollisionDomainEvent(doorId));
        _processedDoorIds.Add(doorIdValue);
        return Succeed();
    }

    private HandleClosedDoorCollisionResult Succeed()
    {
        return new HandleClosedDoorCollisionResult(true, null, null);
    }
}
