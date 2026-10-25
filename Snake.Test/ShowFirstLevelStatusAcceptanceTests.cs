using Snake.UseCases;

namespace Snake.Test;

public class ShowFirstLevelStatusAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-FIRST-LEVEL-STATUS-001")]
    public void SCN_FIRST_LEVEL_STATUS_001_ReturnsThreeOfTwentyAndFifteenPercent()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();

        ShowFirstLevelStatusResult result = fixture.StatusUseCase.Execute(new ShowFirstLevelStatusQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("第一關", result.Data.LevelName);
        Assert.Equal(3, result.Data.CurrentLength);
        Assert.Equal(20, result.Data.TargetLength);
        Assert.Equal(15, result.Data.ProgressPercent);
        Assert.True(fixture.ReadyBoardUseCase.Execute(new ShowFirstLevelReadyBoardQuery()).IsSuccess);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-FIRST-LEVEL-STATUS-002")]
    public void SCN_FIRST_LEVEL_STATUS_002_ReturnsFailureOutsideReadyPhase()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();
        fixture.StartUseCase.Execute(new StartFirstLevelCommand());

        ShowFirstLevelStatusResult result = fixture.StatusUseCase.Execute(new ShowFirstLevelStatusQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_GAME_PHASE", result.FailureCode);
        Assert.Equal("目前不是第一關待開始階段。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(1, fixture.OutputPort.ActivationCount);
    }
}
