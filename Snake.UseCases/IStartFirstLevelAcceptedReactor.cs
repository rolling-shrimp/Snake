using Snake.Entities;

namespace Snake.UseCases;

public interface IStartFirstLevelAcceptedReactor
{
    ActivateFirstLevelPlayResult React(IDomainEvent domainEvent);
}
