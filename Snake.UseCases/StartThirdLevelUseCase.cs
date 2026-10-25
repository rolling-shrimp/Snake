using Snake.Entities;

namespace Snake.UseCases;

public class StartThirdLevelUseCase
{
    private SnakeGameSession _session;
    private IStartThirdLevelAcceptedReactor _reactor;
    private bool _hasAcceptedCommand;

    public StartThirdLevelUseCase(
        SnakeGameSession session,
        IStartThirdLevelAcceptedReactor reactor)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _reactor = reactor ?? throw new ArgumentNullException(nameof(reactor));
    }

    public StartThirdLevelResult Execute(StartThirdLevelCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_hasAcceptedCommand
            || _session.Game.Phase != GamePhase.Ready
            || _session.Level.Number != LevelNumber.Third)
        {
            return Succeed();
        }

        _hasAcceptedCommand = true;
        ActivateThirdLevelPlayResult result = _reactor.React(new StartThirdLevelAcceptedDomainEvent());
        if (!result.IsSuccess)
        {
            _hasAcceptedCommand = false;
            return new StartThirdLevelResult(false, result.FailureCode, result.FailureMessage);
        }

        return Succeed();
    }

    private StartThirdLevelResult Succeed()
    {
        return new StartThirdLevelResult(true, null, null);
    }
}
