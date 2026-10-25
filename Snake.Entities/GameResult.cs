namespace Snake.Entities;

public class GameResult
{
    private GameResultId _gameResultId;
    private List<LevelResult> _levelResults;

    public GameResult(GameResultId gameResultId)
    {
        _gameResultId = gameResultId ?? throw new ArgumentNullException(nameof(gameResultId));
        _levelResults = new List<LevelResult>();
    }

    public GameResultId GameResultId => _gameResultId;
    public List<LevelResult> LevelResults => new List<LevelResult>(_levelResults);
    public LevelResult? LatestResult => _levelResults.LastOrDefault();

    public bool Record(
        LevelResultId levelResultId,
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
            levelResultId,
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
