using Snake.Entities;

namespace Snake.UseCases;

public class RestartGameAcceptedReactor : IRestartGameAcceptedReactor
{
    private ResetGameForReplayUseCase _resetUseCase;
    private HashSet<IDomainEvent> _processedEvents;

    public RestartGameAcceptedReactor(ResetGameForReplayUseCase resetUseCase)
    {
        _resetUseCase = resetUseCase ?? throw new ArgumentNullException(nameof(resetUseCase));
        _processedEvents = new HashSet<IDomainEvent>();
    }

    public ResetGameForReplayResult React(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        if (domainEvent is not RestartGameAcceptedDomainEvent
            || _processedEvents.Contains(domainEvent))
        {
            return new ResetGameForReplayResult(true, null, null);
        }

        ResetGameForReplayResult result = _resetUseCase.Execute(new ResetGameForReplayCommand());
        if (result.IsSuccess)
        {
            _processedEvents.Add(domainEvent);
        }

        return result;
    }
}
