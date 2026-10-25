using Snake.UseCases;

namespace Snake.Test;

public class FirstLevelTestFixture
{
    public FirstLevelTestFixture()
    {
        Session = new SnakeGameSession();
        OutputPort = new RecordingFirstLevelPlayOutputPort();
        ReadyBoardUseCase = new ShowFirstLevelReadyBoardUseCase(Session, new FirstLevelReadyBoardProjection());
        RulesUseCase = new ShowFirstLevelRulesUseCase(Session, new FirstLevelRulesProjection());
        StatusUseCase = new ShowFirstLevelStatusUseCase(Session, new FirstLevelStatusProjection());
        ActivateUseCase = new ActivateFirstLevelPlayUseCase(Session, OutputPort);
        Reactor = new StartFirstLevelAcceptedReactor(ActivateUseCase);
        StartUseCase = new StartFirstLevelUseCase(Session, Reactor);
    }

    public SnakeGameSession Session { get; }
    public RecordingFirstLevelPlayOutputPort OutputPort { get; }
    public ShowFirstLevelReadyBoardUseCase ReadyBoardUseCase { get; }
    public ShowFirstLevelRulesUseCase RulesUseCase { get; }
    public ShowFirstLevelStatusUseCase StatusUseCase { get; }
    public ActivateFirstLevelPlayUseCase ActivateUseCase { get; }
    public StartFirstLevelAcceptedReactor Reactor { get; }
    public StartFirstLevelUseCase StartUseCase { get; }
}
