namespace Snake.Entities;

public class FatalFireDomainEvent : IDomainEvent
{
    public FatalFireDomainEvent(int previousLength, int effectiveLength)
    {
        PreviousLength = previousLength;
        EffectiveLength = effectiveLength;
    }

    public int PreviousLength { get; }
    public int EffectiveLength { get; }
}
