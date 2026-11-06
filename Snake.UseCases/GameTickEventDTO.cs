namespace Snake.UseCases;

public class GameTickEventDTO
{
    public GameTickEventDTO(
        long dueElapsedMilliseconds,
        int sequence,
        GameTickEventType eventType,
        string outcome)
    {
        if (string.IsNullOrWhiteSpace(outcome))
        {
            throw new ArgumentException("事件結果不得為空。", nameof(outcome));
        }

        DueElapsedMilliseconds = dueElapsedMilliseconds;
        Sequence = sequence;
        EventType = eventType;
        Outcome = outcome;
    }

    public long DueElapsedMilliseconds { get; }
    public int Sequence { get; }
    public GameTickEventType EventType { get; }
    public string Outcome { get; }
}
