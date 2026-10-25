using Snake.Entities;

namespace Snake.UseCases;

public interface IRestartGameAcceptedReactor
{
    ResetGameForReplayResult React(IDomainEvent domainEvent);
}
