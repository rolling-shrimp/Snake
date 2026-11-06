namespace Snake.UseCases;

public class ScheduledGameEvent
{
    public ScheduledGameEvent(long dueElapsedMilliseconds, GameTickEventType eventType)
    {
        DueElapsedMilliseconds = dueElapsedMilliseconds;
        EventType = eventType;
    }

    public long DueElapsedMilliseconds { get; }
    public GameTickEventType EventType { get; }
}
