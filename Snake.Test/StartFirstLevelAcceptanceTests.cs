using Snake.UseCases;

namespace Snake.Test;

public class StartFirstLevelAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-START-FIRST-LEVEL-001")]
    public void SCN_START_FIRST_LEVEL_001_AcceptsOnceAndHandsOffWithoutPartialPlayingState()
    {
        SnakeGameSession session = new SnakeGameSession();
        RecordingStartFirstLevelAcceptedReactor reactor = new RecordingStartFirstLevelAcceptedReactor();
        StartFirstLevelUseCase useCase = new StartFirstLevelUseCase(session, reactor);
        ShowFirstLevelReadyBoardUseCase readyBoardUseCase = new ShowFirstLevelReadyBoardUseCase(session, new FirstLevelReadyBoardProjection());

        StartFirstLevelResult result = useCase.Execute(new StartFirstLevelCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, reactor.ReactionCount);
        Assert.NotNull(reactor.LastEvent);
        Assert.True(readyBoardUseCase.Execute(new ShowFirstLevelReadyBoardQuery()).IsSuccess);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-START-FIRST-LEVEL-002")]
    public void SCN_START_FIRST_LEVEL_002_IgnoresCommandOutsideReadyPhase()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();
        fixture.StartUseCase.Execute(new StartFirstLevelCommand());
        RecordingStartFirstLevelAcceptedReactor reactor = new RecordingStartFirstLevelAcceptedReactor();
        StartFirstLevelUseCase useCase = new StartFirstLevelUseCase(fixture.Session, reactor);

        StartFirstLevelResult result = useCase.Execute(new StartFirstLevelCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, reactor.ReactionCount);
        Assert.Equal(1, fixture.OutputPort.ActivationCount);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-START-FIRST-LEVEL-003")]
    public void SCN_START_FIRST_LEVEL_003_IgnoresRepeatedCommandInSameReadyState()
    {
        SnakeGameSession session = new SnakeGameSession();
        RecordingStartFirstLevelAcceptedReactor reactor = new RecordingStartFirstLevelAcceptedReactor();
        StartFirstLevelUseCase useCase = new StartFirstLevelUseCase(session, reactor);

        StartFirstLevelResult firstResult = useCase.Execute(new StartFirstLevelCommand());
        StartFirstLevelResult secondResult = useCase.Execute(new StartFirstLevelCommand());

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Equal(1, reactor.ReactionCount);
    }
}
