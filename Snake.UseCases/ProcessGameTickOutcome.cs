namespace Snake.UseCases;

public enum ProcessGameTickOutcome
{
    NotPlaying,
    NoDueEvents,
    EventsProcessed,
    LevelReady,
    ShieldRescued,
    GameOver,
    GameCompleted,
    Failed
}
