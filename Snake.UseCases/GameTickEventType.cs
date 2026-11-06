namespace Snake.UseCases;

public enum GameTickEventType
{
    DoorClosingWarning,
    DoorToggle,
    DoorCloseDeferred,
    DoorClosedAfterDeparture,
    SnakeMoved,
    ObjectContact,
    ShieldRescued,
    LevelPassed,
    GameOver,
    GameCompleted,
    ObjectReplenished
}
