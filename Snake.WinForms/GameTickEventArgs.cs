namespace Snake.WinForms;

public class GameTickEventArgs : EventArgs
{
    public GameTickEventArgs(long accumulatedElapsedMilliseconds)
    {
        AccumulatedElapsedMilliseconds = accumulatedElapsedMilliseconds;
    }

    public long AccumulatedElapsedMilliseconds { get; }
}
