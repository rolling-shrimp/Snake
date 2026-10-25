using Snake.Entities;

namespace Snake.UseCases;

public class StartSecondLevelUseCase
{
    private SnakeGameSession _session;
    private IStartSecondLevelAcceptedReactor _reactor;
    private bool _hasAcceptedCommand;

    public StartSecondLevelUseCase(
        SnakeGameSession session,
        IStartSecondLevelAcceptedReactor reactor)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _reactor = reactor ?? throw new ArgumentNullException(nameof(reactor));
    }

    public StartSecondLevelResult Execute(StartSecondLevelCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_hasAcceptedCommand
            || _session.Game.Phase != GamePhase.Ready
            || _session.Level.Number != LevelNumber.Second)
        {
            return Succeed();
        }

        _hasAcceptedCommand = true;
        ActivateSecondLevelPlayResult result = _reactor.React(new StartSecondLevelAcceptedDomainEvent());
        if (!result.IsSuccess)
        {
            _hasAcceptedCommand = false;
            return new StartSecondLevelResult(false, result.FailureCode, result.FailureMessage);
        }

        return Succeed();
    }

    private StartSecondLevelResult Succeed()
    {
        return new StartSecondLevelResult(true, null, null);
    }
}
