using Snake.Entities;

namespace Snake.UseCases;

public interface IStartSecondLevelAcceptedReactor
{
    ActivateSecondLevelPlayResult React(IDomainEvent domainEvent);
}
