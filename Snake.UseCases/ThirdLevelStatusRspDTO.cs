namespace Snake.UseCases;

public class ThirdLevelStatusRspDTO
{
    public ThirdLevelStatusRspDTO(
        string levelName,
        int currentLength,
        int targetLength,
        int progressPercent,
        int shieldRemainingCharges)
    {
        LevelName = levelName;
        CurrentLength = currentLength;
        TargetLength = targetLength;
        ProgressPercent = progressPercent;
        ShieldRemainingCharges = shieldRemainingCharges;
    }

    public string LevelName { get; }
    public int CurrentLength { get; }
    public int TargetLength { get; }
    public int ProgressPercent { get; }
    public int ShieldRemainingCharges { get; }
}
