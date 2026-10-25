using Snake.UseCases;

namespace Snake.Test;

public class ShowFirstLevelReadyBoardAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-READY-BOARD-001")]
    public void SCN_READY_BOARD_001_ReturnsReadyBoardWithoutChangingState()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();

        ShowFirstLevelReadyBoardResult result = fixture.ReadyBoardUseCase.Execute(new ShowFirstLevelReadyBoardQuery());
        StartFirstLevelResult startResult = fixture.StartUseCase.Execute(new StartFirstLevelCommand());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(10, result.Data.BoardRows);
        Assert.Equal(10, result.Data.BoardColumns);
        Assert.Equal(3, result.Data.SnakeCells.Count);
        Assert.Equal(3, result.Data.SnakeCells.Select(cell => (cell.X, cell.Y)).Distinct().Count());
        Assert.All(result.Data.SnakeCells, cell =>
        {
            Assert.InRange(cell.X, 0, 9);
            Assert.InRange(cell.Y, 0, 9);
        });
        Assert.True(result.Data.IsStartButtonVisible);
        Assert.True(startResult.IsSuccess);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-READY-BOARD-002")]
    public void SCN_READY_BOARD_002_ReturnsFailureOutsideReadyPhase()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();
        fixture.StartUseCase.Execute(new StartFirstLevelCommand());

        ShowFirstLevelReadyBoardResult result = fixture.ReadyBoardUseCase.Execute(new ShowFirstLevelReadyBoardQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_GAME_PHASE", result.FailureCode);
        Assert.Equal("目前不是第一關待開始階段。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(1, fixture.OutputPort.ActivationCount);
    }
}
