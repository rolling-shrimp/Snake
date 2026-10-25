using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class ShowSecondLevelInformationAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-SECOND-LEVEL-READY-BOARD-001")]
    public void SCN_SHOW_SECOND_LEVEL_READY_BOARD_001_ReturnsOnlyReadyBoardState()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        ShowSecondLevelReadyBoardUseCase useCase = new ShowSecondLevelReadyBoardUseCase(session);

        ShowSecondLevelReadyBoardResult result = useCase.Execute(new ShowSecondLevelReadyBoardQuery());

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
        Assert.Empty(session.GetObjectOccupiedCells());
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-SECOND-LEVEL-READY-BOARD-002")]
    public void SCN_SHOW_SECOND_LEVEL_READY_BOARD_002_RejectsUnavailableStateWithoutData()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second);
        ShowSecondLevelReadyBoardUseCase useCase = new ShowSecondLevelReadyBoardUseCase(session);

        ShowSecondLevelReadyBoardResult result = useCase.Execute(new ShowSecondLevelReadyBoardQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_GAME_PHASE", result.FailureCode);
        Assert.Equal("目前不是第二關待開始階段。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-SECOND-LEVEL-RULES-001")]
    public void SCN_SHOW_SECOND_LEVEL_RULES_001_ReturnsFourOrderedRules()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        ShowSecondLevelRulesUseCase useCase = new ShowSecondLevelRulesUseCase(session);

        ShowSecondLevelRulesResult result = useCase.Execute(new ShowSecondLevelRulesQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Collection(
            result.Data.RuleHints,
            rule => Assert.Equal(("ORDINARY_FOOD_GROWTH", "普通食物 +1 格"), (rule.Code, rule.Text)),
            rule => Assert.Equal(("SPECIAL_FOOD_GROWTH", "特殊食物 +2 格"), (rule.Code, rule.Text)),
            rule => Assert.Equal(("SHIELD_RESCUE", "護盾 +1 次保命"), (rule.Code, rule.Text)),
            rule => Assert.Equal(("FIRE_SHRINK", "火焰 −2 格"), (rule.Code, rule.Text)));
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-SECOND-LEVEL-RULES-002")]
    public void SCN_SHOW_SECOND_LEVEL_RULES_002_RejectsUnavailableStateWithoutData()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Third);
        ShowSecondLevelRulesUseCase useCase = new ShowSecondLevelRulesUseCase(session);

        ShowSecondLevelRulesResult result = useCase.Execute(new ShowSecondLevelRulesQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_GAME_PHASE", result.FailureCode);
        Assert.Equal("目前不是第二關待開始階段。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-SECOND-LEVEL-STATUS-001")]
    public void SCN_SHOW_SECOND_LEVEL_STATUS_001_ReturnsSecondLevelProgress()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.Second);
        ShowSecondLevelStatusUseCase useCase = new ShowSecondLevelStatusUseCase(session);

        ShowSecondLevelStatusResult result = useCase.Execute(new ShowSecondLevelStatusQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("第二關", result.Data.LevelName);
        Assert.Equal(3, result.Data.CurrentLength);
        Assert.Equal(20, result.Data.TargetLength);
        Assert.Equal(15, result.Data.ProgressPercent);
        Assert.Equal(0, result.Data.ShieldRemainingCharges);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SHOW-SECOND-LEVEL-STATUS-002")]
    public void SCN_SHOW_SECOND_LEVEL_STATUS_002_RejectsUnavailableStateWithoutData()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreateReady(LevelNumber.First);
        ShowSecondLevelStatusUseCase useCase = new ShowSecondLevelStatusUseCase(session);

        ShowSecondLevelStatusResult result = useCase.Execute(new ShowSecondLevelStatusQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_GAME_PHASE", result.FailureCode);
        Assert.Equal("目前不是第二關待開始階段。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(GamePhase.Ready, session.Game.Phase);
    }
}
