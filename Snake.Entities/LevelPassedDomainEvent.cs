namespace Snake.Entities;

public class LevelPassedDomainEvent : IDomainEvent
{
    public LevelPassedDomainEvent(LevelNumber levelNumber, int actualLength)
    {
        LevelNumber = levelNumber;
        ActualLength = actualLength;
    }

    public LevelNumber LevelNumber { get; }
    public int ActualLength { get; }
}
