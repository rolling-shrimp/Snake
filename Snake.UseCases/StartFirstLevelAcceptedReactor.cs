using Snake.Entities;

namespace Snake.UseCases;

public class StartFirstLevelAcceptedReactor : IStartFirstLevelAcceptedReactor
{
    private ActivateFirstLevelPlayUseCase _activateFirstLevelPlayUseCase;
    private HashSet<IDomainEvent> _processedEvents;
    private object _syncRoot;

    public StartFirstLevelAcceptedReactor(ActivateFirstLevelPlayUseCase activateFirstLevelPlayUseCase)
    {
        _activateFirstLevelPlayUseCase = activateFirstLevelPlayUseCase;
        _processedEvents = new HashSet<IDomainEvent>();
        _syncRoot = new object();
    }

    public ActivateFirstLevelPlayResult React(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        lock (_syncRoot)
        {
            if (domainEvent is not StartFirstLevelAcceptedDomainEvent)
            {
                return new ActivateFirstLevelPlayResult(true, null, null);
            }

            if (_processedEvents.Contains(domainEvent))
            {
                return new ActivateFirstLevelPlayResult(true, null, null);
            }

            try
            {
                ActivateFirstLevelPlayResult result = _activateFirstLevelPlayUseCase.Execute(new ActivateFirstLevelPlayCommand());
                if (result.IsSuccess)
                {
                    _processedEvents.Add(domainEvent);
                }

                return result;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("StartFirstLevelAcceptedReactor 反應失敗。", exception);
            }
        }
    }
}
