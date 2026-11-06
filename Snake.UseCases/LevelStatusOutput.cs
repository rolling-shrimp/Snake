namespace Snake.UseCases;

public class LevelStatusOutput
{
    public LevelStatusOutput(
        int levelNumber,
        int currentLength,
        int targetLength,
        int progressPercentage,
        int remainingShieldCharges)
    {
        LevelNumber = levelNumber;
        CurrentLength = currentLength;
        TargetLength = targetLength;
        ProgressPercentage = progressPercentage;
        RemainingShieldCharges = remainingShieldCharges;
    }

    public int LevelNumber { get; }
    public int CurrentLength { get; }
    public int TargetLength { get; }
    public int ProgressPercentage { get; }
    public int RemainingShieldCharges { get; }
}
