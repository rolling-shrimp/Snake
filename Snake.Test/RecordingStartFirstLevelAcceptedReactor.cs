using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class RecordingStartFirstLevelAcceptedReactor : IStartFirstLevelAcceptedReactor
{
    private int _reactionCount;
    private IDomainEvent? _lastEvent;

    public int ReactionCount => _reactionCount;
    public IDomainEvent? LastEvent => _lastEvent;

    public ActivateFirstLevelPlayResult React(IDomainEvent domainEvent)
    {
        _reactionCount++;
        _lastEvent = domainEvent;
        return new ActivateFirstLevelPlayResult(true, null, null);
    }
}
