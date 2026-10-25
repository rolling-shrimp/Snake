using Snake.Entities;

namespace Snake.UseCases;

public class EvaluateLevelCompletionUseCase
{
    private SnakeGameSession _session;
    private HashSet<LevelNumber> _publishedLevels;

    public EvaluateLevelCompletionUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _publishedLevels = new HashSet<LevelNumber>();
    }

    public EvaluateLevelCompletionResult Execute(EvaluateLevelCompletionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_session.Game.MovementActive
            || _session.Snake.Length < _session.Level.TargetLength
            || _publishedLevels.Contains(_session.Level.Number))
        {
            return Succeed();
        }

        _session.PublishFact(new LevelPassedDomainEvent(
            _session.Level.Number,
            _session.Snake.Length));
        _publishedLevels.Add(_session.Level.Number);
        return Succeed();
    }

    private EvaluateLevelCompletionResult Succeed()
    {
        return new EvaluateLevelCompletionResult(true, null, null);
    }
}
