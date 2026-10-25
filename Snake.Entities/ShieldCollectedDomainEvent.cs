namespace Snake.Entities;

public class ShieldCollectedDomainEvent : IDomainEvent
{
    public ShieldCollectedDomainEvent(ShieldItemId shieldItemId)
    {
        ShieldItemId = shieldItemId ?? throw new ArgumentNullException(nameof(shieldItemId));
    }

    public ShieldItemId ShieldItemId { get; }
}
