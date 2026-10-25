namespace Snake.UseCases;

public class EndCurrentLevelPlayCommand
{
    public EndCurrentLevelPlayCommand(LevelOutcome outcome, DeathCause? deathCause)
    {
        Outcome = outcome;
        DeathCause = deathCause;
    }

    public LevelOutcome Outcome { get; }
    public DeathCause? DeathCause { get; }
}
