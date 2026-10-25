namespace Snake.UseCases;

public class SetSnakeDirectionCommand
{
    public SetSnakeDirectionCommand(Direction direction)
    {
        Direction = direction;
    }

    public Direction Direction { get; }
}
