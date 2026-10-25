using Snake.Entities;

namespace Snake.UseCases;

public class HandleShieldContactUseCase
{
    private SnakeGameSession _session;

    public HandleShieldContactUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public HandleShieldContactResult Execute(HandleShieldContactCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_session.Game.MovementActive
            || _session.Level.Number == LevelNumber.First
            || !Guid.TryParse(command.ShieldItemId, out Guid itemIdValue))
        {
            return Succeed();
        }

        ShieldItemId shieldItemId = new ShieldItemId(itemIdValue);
        ShieldItem? shieldItem = _session.Shield.ActiveItem;
        if (shieldItem is null
            || !shieldItem.ShieldItemId.Equals(shieldItemId)
            || !shieldItem.Position.Equals(_session.Snake.BodyCells[0]))
        {
            return Succeed();
        }

        if (_session.Shield.Collect(shieldItemId))
        {
            _session.PublishFact(new ShieldCollectedDomainEvent(shieldItemId));
        }

        return Succeed();
    }

    private HandleShieldContactResult Succeed()
    {
        return new HandleShieldContactResult(true, null, null);
    }
}
