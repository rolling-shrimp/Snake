namespace Snake.UseCases;

public class RescueWithShieldCommand
{
    public RescueWithShieldCommand(DeathCause deathCause, int preHitLength)
    {
        DeathCause = deathCause;
        PreHitLength = preHitLength;
    }

    public DeathCause DeathCause { get; }
    public int PreHitLength { get; }
}
