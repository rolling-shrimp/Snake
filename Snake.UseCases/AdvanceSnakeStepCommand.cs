namespace Snake.UseCases;

public class AdvanceSnakeStepCommand
{
    public AdvanceSnakeStepCommand(int elapsedMilliseconds)
    {
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public int ElapsedMilliseconds { get; }
}
