using Snake.UseCases;

namespace Snake.WinForms;

public class DirectionRequestedEventArgs : EventArgs
{
    public DirectionRequestedEventArgs(Direction direction)
    {
        Direction = direction;
    }

    public Direction Direction { get; }
}
