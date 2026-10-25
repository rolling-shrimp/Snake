namespace Snake.UseCases;

public class UpdateThirdLevelDoorStateCommand
{
    public UpdateThirdLevelDoorStateCommand(int elapsedMilliseconds)
    {
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public int ElapsedMilliseconds { get; }
}
