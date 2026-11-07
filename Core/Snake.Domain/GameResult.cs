namespace Snake.Domain
{

public class GameResult
{
    private List<LevelResult> _levelResults;

    public GameResult()
    {
        _levelResults = new List<LevelResult>();
    }

    public List<LevelResult> LevelResults => new List<LevelResult>(_levelResults);
    public LevelResult? LatestResult => _levelResults.LastOrDefault();

    internal GameResult Copy()
    {
        GameResult copy = new GameResult();
        copy._levelResults = new List<LevelResult>(_levelResults);
        return copy;
    }

    public bool Record(
        LevelNumber level,
        LevelOutcome outcome,
        int finalLength,
        DeathCause? deathCause)
    {
        if (_levelResults.Any(result => result.Level == level))
        {
            return false;
        }

        _levelResults.Add(new LevelResult(
            level,
            outcome,
            finalLength,
            deathCause));
        return true;
    }

    public void Reset()
    {
        _levelResults.Clear();
    }
}
}
