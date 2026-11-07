namespace Snake.Domain
{

public class LevelResult
{
    private LevelNumber _level;
    private LevelOutcome _outcome;
    private int _finalLength;
    private DeathCause? _deathCause;

    public LevelResult(
        LevelNumber level,
        LevelOutcome outcome,
        int finalLength,
        DeathCause? deathCause)
    {
        if (finalLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(finalLength));
        }

        if (outcome == LevelOutcome.Passed && deathCause is not null)
        {
            throw new ArgumentException("Passed 結果不得包含死亡原因。", nameof(deathCause));
        }

        if (outcome == LevelOutcome.Failed && deathCause is null)
        {
            throw new ArgumentException("Failed 結果必須包含死亡原因。", nameof(deathCause));
        }

        _level = level;
        _outcome = outcome;
        _finalLength = finalLength;
        _deathCause = deathCause;
    }

    public LevelNumber Level => _level;
    public LevelOutcome Outcome => _outcome;
    public int FinalLength => _finalLength;
    public DeathCause? DeathCause => _deathCause;
}
}
