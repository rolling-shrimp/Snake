using Snake.Entities;

namespace Snake.UseCases;

public interface IStartThirdLevelAcceptedReactor
{
    ActivateThirdLevelPlayResult React(IDomainEvent domainEvent);
}
