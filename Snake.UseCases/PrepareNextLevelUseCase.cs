using Snake.Entities;

namespace Snake.UseCases;

public class PrepareNextLevelUseCase
{
    private SnakeGameSession _session;
    private HashSet<int> _processedLevels;

    public PrepareNextLevelUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _processedLevels = new HashSet<int>();
    }

    public PrepareNextLevelResult Execute(PrepareNextLevelCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.CompletedLevel < 1
            || command.CompletedLevel > 2
            || _processedLevels.Contains(command.CompletedLevel)
            || (int)_session.Level.Number != command.CompletedLevel)
        {
            return Succeed();
        }

        LevelResult? completedResult = _session.GameResult.LevelResults.FirstOrDefault(result =>
            (int)result.Level == command.CompletedLevel);
        if (completedResult is null || completedResult.Outcome != Snake.Entities.LevelOutcome.Passed)
        {
            return Succeed();
        }

        try
        {
            if (!_session.Level.Advance())
            {
                return Succeed();
            }

            _session.Snake.Reset(_session.Board.GetFirstLevelSafeStart());
            _session.Food.Reset();
            _session.Shield.Reset();
            _session.Fire.Reset();
            _session.Bomb.Reset();
            _session.DoorAndMaze.Reset();
            if (_session.Level.Number == LevelNumber.Third)
            {
                _session.DoorAndMaze.PrepareThirdLevel(new DoorId(Guid.NewGuid()));
            }

            _session.Game.PrepareNextLevel();
            _processedLevels.Add(command.CompletedLevel);
            return Succeed();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("PrepareNextLevelUseCase 執行失敗。", exception);
        }
    }

    private PrepareNextLevelResult Succeed()
    {
        return new PrepareNextLevelResult(true, null, null);
    }
}
