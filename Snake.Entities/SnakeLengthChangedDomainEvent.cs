namespace Snake.Entities;

public class SnakeLengthChangedDomainEvent : IDomainEvent
{
    public SnakeLengthChangedDomainEvent(int previousLength, int currentLength)
    {
        PreviousLength = previousLength;
        CurrentLength = currentLength;
    }

    public int PreviousLength { get; }
    public int CurrentLength { get; }
}
