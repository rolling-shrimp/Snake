namespace Snake.UseCases;

public class LevelStatusSourceDTO
{
    public LevelStatusSourceDTO(
        int levelNumber,
        int currentLength,
        int targetLength,
        int? shieldRemainingCharges)
    {
        LevelNumber = levelNumber;
        CurrentLength = currentLength;
        TargetLength = targetLength;
        ShieldRemainingCharges = shieldRemainingCharges;
    }

    public int LevelNumber { get; }
    public int CurrentLength { get; }
    public int TargetLength { get; }
    public int? ShieldRemainingCharges { get; }
}
