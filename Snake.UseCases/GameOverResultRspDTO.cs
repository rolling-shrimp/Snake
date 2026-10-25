namespace Snake.UseCases;

public class GameOverResultRspDTO
{
    public GameOverResultRspDTO(
        int failedLevel,
        int finalLength,
        DeathCause deathCause,
        int progressPercent,
        bool isReplayButtonVisible)
    {
        FailedLevel = failedLevel;
        FinalLength = finalLength;
        DeathCause = deathCause;
        ProgressPercent = progressPercent;
        IsReplayButtonVisible = isReplayButtonVisible;
    }

    public int FailedLevel { get; }
    public int FinalLength { get; }
    public DeathCause DeathCause { get; }
    public int ProgressPercent { get; }
    public bool IsReplayButtonVisible { get; }
}
