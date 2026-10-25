namespace Snake.UseCases;

public class FirstLevelMovementDTO
{
    public FirstLevelMovementDTO(bool isActive, int intervalMilliseconds, int? levelTimeLimitMilliseconds)
    {
        IsActive = isActive;
        IntervalMilliseconds = intervalMilliseconds;
        LevelTimeLimitMilliseconds = levelTimeLimitMilliseconds;
    }

    public bool IsActive { get; }
    public int IntervalMilliseconds { get; }
    public int? LevelTimeLimitMilliseconds { get; }
}
