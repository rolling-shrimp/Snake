using Snake.Entities;

namespace Snake.UseCases;

public class StartThirdLevelAcceptedReactor : IStartThirdLevelAcceptedReactor
{
    private ActivateThirdLevelPlayUseCase _activateUseCase;
    private HashSet<IDomainEvent> _processedEvents;

    public StartThirdLevelAcceptedReactor(ActivateThirdLevelPlayUseCase activateUseCase)
    {
        _activateUseCase = activateUseCase ?? throw new ArgumentNullException(nameof(activateUseCase));
        _processedEvents = new HashSet<IDomainEvent>();
    }

    public ActivateThirdLevelPlayResult React(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (domainEvent is not StartThirdLevelAcceptedDomainEvent
            || _processedEvents.Contains(domainEvent))
        {
            return new ActivateThirdLevelPlayResult(true, null, null);
        }

        ActivateThirdLevelPlayResult result = _activateUseCase.Execute(new ActivateThirdLevelPlayCommand());
        if (result.IsSuccess)
        {
            _processedEvents.Add(domainEvent);
        }

        return result;
    }
}
