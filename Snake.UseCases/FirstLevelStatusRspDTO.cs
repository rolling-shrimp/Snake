namespace Snake.UseCases;

public class FirstLevelStatusRspDTO
{
    public FirstLevelStatusRspDTO(string levelName, int currentLength, int targetLength, int progressPercent)
    {
        LevelName = levelName;
        CurrentLength = currentLength;
        TargetLength = targetLength;
        ProgressPercent = progressPercent;
    }

    public string LevelName { get; }
    public int CurrentLength { get; }
    public int TargetLength { get; }
    public int ProgressPercent { get; }
}
