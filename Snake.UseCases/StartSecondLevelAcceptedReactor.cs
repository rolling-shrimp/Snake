using Snake.Entities;

namespace Snake.UseCases;

public class StartSecondLevelAcceptedReactor : IStartSecondLevelAcceptedReactor
{
    private ActivateSecondLevelPlayUseCase _activateUseCase;
    private HashSet<IDomainEvent> _processedEvents;

    public StartSecondLevelAcceptedReactor(ActivateSecondLevelPlayUseCase activateUseCase)
    {
        _activateUseCase = activateUseCase ?? throw new ArgumentNullException(nameof(activateUseCase));
        _processedEvents = new HashSet<IDomainEvent>();
    }

    public ActivateSecondLevelPlayResult React(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (domainEvent is not StartSecondLevelAcceptedDomainEvent
            || _processedEvents.Contains(domainEvent))
        {
            return new ActivateSecondLevelPlayResult(true, null, null);
        }

        ActivateSecondLevelPlayResult result = _activateUseCase.Execute(new ActivateSecondLevelPlayCommand());
        if (result.IsSuccess)
        {
            _processedEvents.Add(domainEvent);
        }

        return result;
    }
}
