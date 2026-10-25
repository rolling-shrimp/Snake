namespace Snake.UseCases;

public class LevelStatusReadModel
{
    public LevelStatusReadModel(
        string levelName,
        int currentLength,
        int targetLength,
        int progressPercent,
        int? shieldRemainingCharges,
        long sourceVersion)
    {
        LevelName = levelName;
        CurrentLength = currentLength;
        TargetLength = targetLength;
        ProgressPercent = progressPercent;
        ShieldRemainingCharges = shieldRemainingCharges;
        SourceVersion = sourceVersion;
    }

    public string LevelName { get; }
    public int CurrentLength { get; }
    public int TargetLength { get; }
    public int ProgressPercent { get; }
    public int? ShieldRemainingCharges { get; }
    public long SourceVersion { get; }
}
