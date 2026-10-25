using Snake.Entities;

namespace Snake.UseCases;

public class RestartGameUseCase
{
    private SnakeGameSession _session;
    private IRestartGameAcceptedReactor _reactor;
    private bool _hasAcceptedCommand;

    public RestartGameUseCase(
        SnakeGameSession session,
        IRestartGameAcceptedReactor reactor)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _reactor = reactor ?? throw new ArgumentNullException(nameof(reactor));
    }

    public RestartGameResult Execute(RestartGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_hasAcceptedCommand
            || (_session.Game.Phase != GamePhase.GameOver
                && _session.Game.Phase != GamePhase.Completed))
        {
            return Succeed();
        }

        _hasAcceptedCommand = true;
        ResetGameForReplayResult result = _reactor.React(new RestartGameAcceptedDomainEvent());
        if (!result.IsSuccess)
        {
            _hasAcceptedCommand = false;
            return new RestartGameResult(false, result.FailureCode, result.FailureMessage);
        }

        return Succeed();
    }

    private RestartGameResult Succeed()
    {
        return new RestartGameResult(true, null, null);
    }
}
