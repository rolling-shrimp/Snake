namespace Snake.UseCases;

public class HandleShieldContactCommand
{
    public HandleShieldContactCommand(string shieldItemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shieldItemId);
        ShieldItemId = shieldItemId;
    }

    public string ShieldItemId { get; }
}
