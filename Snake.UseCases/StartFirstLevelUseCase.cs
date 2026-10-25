using Snake.Entities;

namespace Snake.UseCases;

public class StartFirstLevelUseCase
{
    private SnakeGameSession _session;
    private IStartFirstLevelAcceptedReactor _reactor;
    private object _syncRoot;
    private bool _hasAcceptedCommand;

    public StartFirstLevelUseCase(SnakeGameSession session, IStartFirstLevelAcceptedReactor reactor)
    {
        _session = session;
        _reactor = reactor;
        _syncRoot = new object();
        _hasAcceptedCommand = false;
    }

    public StartFirstLevelResult Execute(StartFirstLevelCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        lock (_syncRoot)
        {
            if (_hasAcceptedCommand || _session.Game.Phase != GamePhase.Ready || _session.Level.Number != LevelNumber.First)
            {
                return new StartFirstLevelResult(true, null, null);
            }

            StartFirstLevelAcceptedDomainEvent? domainEvent = _session.Game.AcceptFirstLevelStart();
            if (domainEvent is null)
            {
                return new StartFirstLevelResult(true, null, null);
            }

            _hasAcceptedCommand = true;
            try
            {
                ActivateFirstLevelPlayResult reactionResult = _reactor.React(domainEvent);
                if (!reactionResult.IsSuccess)
                {
                    _hasAcceptedCommand = false;
                    return new StartFirstLevelResult(false, reactionResult.FailureCode, reactionResult.FailureMessage);
                }

                return new StartFirstLevelResult(true, null, null);
            }
            catch (Exception exception)
            {
                _hasAcceptedCommand = false;
                throw new InvalidOperationException("StartFirstLevelUseCase 執行失敗。", exception);
            }
        }
    }
}
