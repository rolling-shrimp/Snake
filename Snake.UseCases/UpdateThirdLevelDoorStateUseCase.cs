using Snake.Entities;

namespace Snake.UseCases;

public class UpdateThirdLevelDoorStateUseCase
{
    private SnakeGameSession _session;

    public UpdateThirdLevelDoorStateUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public UpdateThirdLevelDoorStateResult Execute(UpdateThirdLevelDoorStateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_session.Game.MovementActive || _session.Level.Number != LevelNumber.Third)
        {
            return Succeed();
        }

        _session.DoorAndMaze.Update(command.ElapsedMilliseconds, _session.Snake.BodyCells);
        return Succeed();
    }

    private UpdateThirdLevelDoorStateResult Succeed()
    {
        return new UpdateThirdLevelDoorStateResult(true, null, null);
    }
}
