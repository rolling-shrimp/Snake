namespace Snake.WinForms;

public class GameTickEventArgs : EventArgs
{
    public GameTickEventArgs(int elapsedMilliseconds)
    {
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public int ElapsedMilliseconds { get; }
}
