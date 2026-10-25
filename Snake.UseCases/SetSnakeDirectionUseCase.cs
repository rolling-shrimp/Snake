using Snake.Entities;

namespace Snake.UseCases;

public class SetSnakeDirectionUseCase
{
    private SnakeGameSession _session;

    public SetSnakeDirectionUseCase(SnakeGameSession session)
    {
        _session = session;
    }

    public SetSnakeDirectionResult Execute(SetSnakeDirectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_session.Game.Phase != GamePhase.Playing)
        {
            return new SetSnakeDirectionResult(true, null, null);
        }

        Snake.Entities.Direction? domainDirection = command.Direction switch
        {
            Direction.Up => Snake.Entities.Direction.Up,
            Direction.Down => Snake.Entities.Direction.Down,
            Direction.Left => Snake.Entities.Direction.Left,
            Direction.Right => Snake.Entities.Direction.Right,
            _ => null
        };
        if (domainDirection is null)
        {
            return new SetSnakeDirectionResult(true, null, null);
        }

        _session.Snake.QueueDirection(domainDirection.Value);
        return new SetSnakeDirectionResult(true, null, null);
    }
}
