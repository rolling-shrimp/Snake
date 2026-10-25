using Snake.Entities;

namespace Snake.UseCases;

public class HandleFireContactUseCase
{
    private SnakeGameSession _session;

    public HandleFireContactUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public HandleFireContactResult Execute(HandleFireContactCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_session.Game.MovementActive
            || _session.Level.Number == LevelNumber.First
            || !Guid.TryParse(command.FireItemId, out Guid itemIdValue))
        {
            return Succeed();
        }

        FireItemId fireItemId = new FireItemId(itemIdValue);
        FireItem? fireItem = _session.Fire.ActiveItem;
        if (fireItem is null
            || !fireItem.FireItemId.Equals(fireItemId)
            || !fireItem.Position.Equals(_session.Snake.BodyCells[0]))
        {
            return Succeed();
        }

        try
        {
            _session.Fire.Consume(fireItemId);
            LengthChangeResult lengthChange = _session.Snake.CalculateLengthChange(_session.Fire.LengthDelta);
            if (lengthChange.IsDepleted)
            {
                _session.PublishFact(new FatalFireDomainEvent(
                    lengthChange.PreviousLength,
                    lengthChange.EffectiveLength));
                return Succeed();
            }

            _session.Snake.CommitReduction(lengthChange);
            _session.PublishFact(new SnakeLengthChangedDomainEvent(
                lengthChange.PreviousLength,
                lengthChange.EffectiveLength));
            return Succeed();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("HandleFireContactUseCase 執行失敗。", exception);
        }
    }

    private HandleFireContactResult Succeed()
    {
        return new HandleFireContactResult(true, null, null);
    }
}
