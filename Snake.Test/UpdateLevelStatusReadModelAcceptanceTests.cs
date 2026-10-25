using Snake.UseCases;

namespace Snake.Test;

public class UpdateLevelStatusReadModelAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-UPDATE-LEVEL-STATUS-READ-MODEL-001")]
    public void SCN_UPDATE_LEVEL_STATUS_READ_MODEL_001_ProjectsFirstLevelThreeOfTwentyAtFifteenPercent()
    {
        StubLevelStatusReadPort readPort = new StubLevelStatusReadPort(
            new LevelStatusSourceDTO(1, 3, 20, null));
        UpdateLevelStatusReadModelUseCase useCase = new UpdateLevelStatusReadModelUseCase(readPort);

        UpdateLevelStatusReadModelResult result = useCase.Execute(
            new UpdateLevelStatusReadModelCommand("event-1", 1));

        Assert.True(result.IsSuccess);
        Assert.NotNull(useCase.Current);
        Assert.Equal("第一關", useCase.Current.LevelName);
        Assert.Equal(3, useCase.Current.CurrentLength);
        Assert.Equal(20, useCase.Current.TargetLength);
        Assert.Equal(15, useCase.Current.ProgressPercent);
        Assert.Null(useCase.Current.ShieldRemainingCharges);
        Assert.Equal(1, useCase.Current.SourceVersion);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-UPDATE-LEVEL-STATUS-READ-MODEL-002")]
    public void SCN_UPDATE_LEVEL_STATUS_READ_MODEL_002_ProjectsThirdLevelShieldAndSixPercent()
    {
        StubLevelStatusReadPort readPort = new StubLevelStatusReadPort(
            new LevelStatusSourceDTO(3, 3, 50, 0));
        UpdateLevelStatusReadModelUseCase useCase = new UpdateLevelStatusReadModelUseCase(readPort);

        UpdateLevelStatusReadModelResult result = useCase.Execute(
            new UpdateLevelStatusReadModelCommand("event-1", 1));

        Assert.True(result.IsSuccess);
        Assert.NotNull(useCase.Current);
        Assert.Equal("第三關", useCase.Current.LevelName);
        Assert.Equal(3, useCase.Current.CurrentLength);
        Assert.Equal(50, useCase.Current.TargetLength);
        Assert.Equal(6, useCase.Current.ProgressPercent);
        Assert.Equal(0, useCase.Current.ShieldRemainingCharges);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-UPDATE-LEVEL-STATUS-READ-MODEL-003")]
    public void SCN_UPDATE_LEVEL_STATUS_READ_MODEL_003_PreservesActualLengthAndClampsProgress()
    {
        StubLevelStatusReadPort completedPort = new StubLevelStatusReadPort(
            new LevelStatusSourceDTO(3, 51, 50, 0));
        UpdateLevelStatusReadModelUseCase completedUseCase = new UpdateLevelStatusReadModelUseCase(completedPort);
        completedUseCase.Execute(new UpdateLevelStatusReadModelCommand("completed", 1));

        Assert.NotNull(completedUseCase.Current);
        Assert.Equal(51, completedUseCase.Current.CurrentLength);
        Assert.Equal(100, completedUseCase.Current.ProgressPercent);

        StubLevelStatusReadPort fatalFirePort = new StubLevelStatusReadPort(
            new LevelStatusSourceDTO(2, 0, 20, 0));
        UpdateLevelStatusReadModelUseCase fatalFireUseCase = new UpdateLevelStatusReadModelUseCase(fatalFirePort);
        fatalFireUseCase.Execute(new UpdateLevelStatusReadModelCommand("fatal-fire", 1));

        Assert.NotNull(fatalFireUseCase.Current);
        Assert.Equal(0, fatalFireUseCase.Current.CurrentLength);
        Assert.Equal(0, fatalFireUseCase.Current.ProgressPercent);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-UPDATE-LEVEL-STATUS-READ-MODEL-004")]
    public void SCN_UPDATE_LEVEL_STATUS_READ_MODEL_004_IgnoresDuplicateAndOlderEvents()
    {
        StubLevelStatusReadPort readPort = new StubLevelStatusReadPort(
            new LevelStatusSourceDTO(1, 3, 20, null));
        UpdateLevelStatusReadModelUseCase useCase = new UpdateLevelStatusReadModelUseCase(readPort);
        useCase.Execute(new UpdateLevelStatusReadModelCommand("event-2", 2));
        LevelStatusReadModel currentBefore = useCase.Current!;
        readPort.Status = new LevelStatusSourceDTO(3, 51, 50, 2);

        UpdateLevelStatusReadModelResult duplicateResult = useCase.Execute(
            new UpdateLevelStatusReadModelCommand("event-2", 3));
        UpdateLevelStatusReadModelResult olderResult = useCase.Execute(
            new UpdateLevelStatusReadModelCommand("event-1", 1));

        Assert.True(duplicateResult.IsSuccess);
        Assert.True(olderResult.IsSuccess);
        Assert.Same(currentBefore, useCase.Current);
        Assert.Equal(1, readPort.ReadCount);
    }
}
