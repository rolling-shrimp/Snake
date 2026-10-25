using Snake.Entities;

namespace Snake.UseCases;

public class AdvanceSnakeStepUseCase
{
    private SnakeGameSession _session;

    public AdvanceSnakeStepUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public AdvanceSnakeStepResult Execute(AdvanceSnakeStepCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_session.Game.MovementActive
            || command.ElapsedMilliseconds < _session.Snake.MovementIntervalMilliseconds)
        {
            return Succeed();
        }

        try
        {
            GridPosition currentHead = _session.Snake.BodyCells[0];
            BoardMovementResult movement = _session.Board.ResolveMovement(
                currentHead,
                _session.Snake.NextDirection,
                _session.Level.BoundaryPolicy);
            if (movement.Position is null)
            {
                _session.RecordMovementFact(new WallCollisionDomainEvent(
                    movement.AttemptedX,
                    movement.AttemptedY));
                return Succeed();
            }

            if (_session.Snake.BodyCells.Contains(movement.Position))
            {
                _session.RecordMovementFact(new SelfCollisionDomainEvent(movement.Position));
                return Succeed();
            }

            if (_session.Level.Number == LevelNumber.Third)
            {
                if (_session.DoorAndMaze.WallCells.Contains(movement.Position))
                {
                    _session.RecordMovementFact(new WallCollisionDomainEvent(
                        movement.Position.X,
                        movement.Position.Y));
                    return Succeed();
                }

                Door? closedDoor = _session.DoorAndMaze.Doors.FirstOrDefault(door =>
                    door.Position.Equals(movement.Position) && door.Phase == DoorPhase.Closed);
                if (closedDoor is not null)
                {
                    _session.RecordMovementFact(new ClosedDoorCollisionDomainEvent(closedDoor.DoorId));
                    return Succeed();
                }
            }

            _session.Snake.MoveTo(movement.Position);
            _session.RecordMovementFact(null);
            return Succeed();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("AdvanceSnakeStepUseCase 執行失敗。", exception);
        }
    }

    private AdvanceSnakeStepResult Succeed()
    {
        return new AdvanceSnakeStepResult(true, null, null);
    }
}
