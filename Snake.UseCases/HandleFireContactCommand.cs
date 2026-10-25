namespace Snake.UseCases;

public class HandleFireContactCommand
{
    public HandleFireContactCommand(string fireItemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fireItemId);
        FireItemId = fireItemId;
    }

    public string FireItemId { get; }
}
