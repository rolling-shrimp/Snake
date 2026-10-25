namespace Snake.UseCases;

public class HandleBombContactCommand
{
    public HandleBombContactCommand(string bombItemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bombItemId);
        BombItemId = bombItemId;
    }

    public string BombItemId { get; }
}
