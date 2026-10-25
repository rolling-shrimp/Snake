using Snake.Entities;

namespace Snake.UseCases;

public class HandleBombContactUseCase
{
    private SnakeGameSession _session;

    public HandleBombContactUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public HandleBombContactResult Execute(HandleBombContactCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_session.Game.MovementActive
            || _session.Level.Number != LevelNumber.Third
            || !Guid.TryParse(command.BombItemId, out Guid itemIdValue))
        {
            return Succeed();
        }

        BombItemId bombItemId = new BombItemId(itemIdValue);
        BombItem? bombItem = _session.Bomb.ActiveItem;
        if (bombItem is null
            || !bombItem.BombItemId.Equals(bombItemId)
            || !bombItem.Position.Equals(_session.Snake.BodyCells[0]))
        {
            return Succeed();
        }

        if (_session.Bomb.Consume(bombItemId))
        {
            _session.PublishFact(new BombCollisionDomainEvent(bombItemId));
        }

        return Succeed();
    }

    private HandleBombContactResult Succeed()
    {
        return new HandleBombContactResult(true, null, null);
    }
}
