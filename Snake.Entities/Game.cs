namespace Snake.Entities;

public class Game
{
    private GameId _gameId;
    private GamePhase _phase;
    private long _lastProcessedElapsedMilliseconds;

    public Game(GameId gameId)
    {
        _gameId = gameId ?? throw new ArgumentNullException(nameof(gameId));
        _phase = GamePhase.Ready;
        _lastProcessedElapsedMilliseconds = 0;
    }

    public GameId GameId => _gameId;
    public GamePhase Phase => _phase;
    public bool CanStart => _phase == GamePhase.Ready;
    public bool MovementActive => _phase == GamePhase.Playing;
    public long LastProcessedElapsedMilliseconds => _lastProcessedElapsedMilliseconds;

    internal Game Copy()
    {
        Game copy = new Game(_gameId);
        copy._phase = _phase;
        copy._lastProcessedElapsedMilliseconds = _lastProcessedElapsedMilliseconds;
        return copy;
    }

    public bool StartLevel()
    {
        if (_phase != GamePhase.Ready)
        {
            return false;
        }

        _phase = GamePhase.Playing;
        _lastProcessedElapsedMilliseconds = 0;
        return true;
    }

    public void CommitTickTime(long accumulatedElapsedMilliseconds)
    {
        if (accumulatedElapsedMilliseconds < 0
            || accumulatedElapsedMilliseconds < _lastProcessedElapsedMilliseconds)
        {
            throw new ArgumentOutOfRangeException(nameof(accumulatedElapsedMilliseconds));
        }

        _lastProcessedElapsedMilliseconds = accumulatedElapsedMilliseconds;
    }

    public void PrepareNextLevel()
    {
        _phase = GamePhase.Ready;
        _lastProcessedElapsedMilliseconds = 0;
    }

    public void Complete()
    {
        if (_phase == GamePhase.Playing)
        {
            _phase = GamePhase.Completed;
        }
    }

    public void EndGame()
    {
        if (_phase == GamePhase.Playing)
        {
            _phase = GamePhase.GameOver;
        }
    }

    public void RestartPlaying()
    {
        if (_phase == GamePhase.GameOver || _phase == GamePhase.Completed)
        {
            _phase = GamePhase.Playing;
            _lastProcessedElapsedMilliseconds = 0;
        }
    }
}
