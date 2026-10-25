namespace Snake.Entities;

public class Game
{
    private GameId _gameId;
    private GamePhase _phase;

    public Game(GameId gameId)
    {
        _gameId = gameId ?? throw new ArgumentNullException(nameof(gameId));
        _phase = GamePhase.Ready;
    }

    public GameId GameId => _gameId;
    public GamePhase Phase => _phase;
    public bool CanStart => _phase == GamePhase.Ready;
    public bool MovementActive => _phase == GamePhase.Playing;

    public StartFirstLevelAcceptedDomainEvent? AcceptFirstLevelStart()
    {
        if (!CanStart)
        {
            return null;
        }

        return new StartFirstLevelAcceptedDomainEvent();
    }

    public void ActivateFirstLevel()
    {
        if (_phase != GamePhase.Ready)
        {
            return;
        }

        _phase = GamePhase.Playing;
    }

    public void ActivateLevel()
    {
        if (_phase != GamePhase.Ready)
        {
            return;
        }

        _phase = GamePhase.Playing;
    }

    public void PrepareNextLevel()
    {
        _phase = GamePhase.Ready;
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
        }
    }
}
