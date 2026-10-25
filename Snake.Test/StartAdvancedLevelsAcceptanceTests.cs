using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class StartAdvancedLevelsAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-START-SECOND-LEVEL-001")]
    public void SCN_START_SECOND_LEVEL_001_AcceptsReadyCommandOnceAndHandsOffActivation()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        ActivateSecondLevelPlayUseCase activateUseCase = new ActivateSecondLevelPlayUseCase(session, outputPort);
        StartSecondLevelAcceptedReactor reactor = new StartSecondLevelAcceptedReactor(activateUseCase);
        StartSecondLevelUseCase useCase = new StartSecondLevelUseCase(session, reactor);

        StartSecondLevelResult firstResult = useCase.Execute(new StartSecondLevelCommand());
        StartSecondLevelResult duplicateResult = useCase.Execute(new StartSecondLevelCommand());

        Assert.True(firstResult.IsSuccess);
        Assert.True(duplicateResult.IsSuccess);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
        Assert.Equal(1, outputPort.Operations.Count(operation => operation == "start-movement"));
    }

    [Fact]
    [Trait("ScenarioId", "SCN-START-SECOND-LEVEL-002")]
    public void SCN_START_SECOND_LEVEL_002_IgnoresWrongReadyState()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.First);
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        StartSecondLevelUseCase useCase = new StartSecondLevelUseCase(
            session,
            new StartSecondLevelAcceptedReactor(new ActivateSecondLevelPlayUseCase(session, outputPort)));

        StartSecondLevelResult result = useCase.Execute(new StartSecondLevelCommand());

        Assert.True(result.IsSuccess);
        Assert.Empty(outputPort.Operations);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-START-THIRD-LEVEL-001")]
    public void SCN_START_THIRD_LEVEL_001_AcceptsReadyCommandOnceAndHandsOffActivation()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Third);
        session.DoorAndMaze.PrepareThirdLevel(new DoorId(Guid.NewGuid()));
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        ActivateThirdLevelPlayUseCase activateUseCase = new ActivateThirdLevelPlayUseCase(session, outputPort);
        StartThirdLevelAcceptedReactor reactor = new StartThirdLevelAcceptedReactor(activateUseCase);
        StartThirdLevelUseCase useCase = new StartThirdLevelUseCase(session, reactor);

        StartThirdLevelResult firstResult = useCase.Execute(new StartThirdLevelCommand());
        StartThirdLevelResult duplicateResult = useCase.Execute(new StartThirdLevelCommand());

        Assert.True(firstResult.IsSuccess);
        Assert.True(duplicateResult.IsSuccess);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
        Assert.Equal(1, outputPort.Operations.Count(operation => operation == "start-door-cycle"));
    }

    [Fact]
    [Trait("ScenarioId", "SCN-START-THIRD-LEVEL-002")]
    public void SCN_START_THIRD_LEVEL_002_IgnoresWrongReadyState()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        StubAdvancedLevelPlayOutputPort outputPort = new StubAdvancedLevelPlayOutputPort();
        StartThirdLevelUseCase useCase = new StartThirdLevelUseCase(
            session,
            new StartThirdLevelAcceptedReactor(new ActivateThirdLevelPlayUseCase(session, outputPort)));

        StartThirdLevelResult result = useCase.Execute(new StartThirdLevelCommand());

        Assert.True(result.IsSuccess);
        Assert.Empty(outputPort.Operations);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }
}
