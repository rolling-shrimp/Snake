namespace Snake.Entities;

public class LengthChangeResult
{
    public LengthChangeResult(
        int previousLength,
        int requestedDelta,
        int effectiveLength,
        bool isDepleted)
    {
        PreviousLength = previousLength;
        RequestedDelta = requestedDelta;
        EffectiveLength = effectiveLength;
        IsDepleted = isDepleted;
    }

    public int PreviousLength { get; }
    public int RequestedDelta { get; }
    public int EffectiveLength { get; }
    public bool IsDepleted { get; }
}
