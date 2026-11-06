namespace Snake.UseCases;

public class ProcessGameTickCommand
{
    public ProcessGameTickCommand(long accumulatedElapsedMilliseconds)
    {
        AccumulatedElapsedMilliseconds = accumulatedElapsedMilliseconds;
    }

    public long AccumulatedElapsedMilliseconds { get; }
}
