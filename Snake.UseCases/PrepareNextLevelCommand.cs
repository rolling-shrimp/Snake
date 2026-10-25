namespace Snake.UseCases;

public class PrepareNextLevelCommand
{
    public PrepareNextLevelCommand(int completedLevel)
    {
        CompletedLevel = completedLevel;
    }

    public int CompletedLevel { get; }
}
