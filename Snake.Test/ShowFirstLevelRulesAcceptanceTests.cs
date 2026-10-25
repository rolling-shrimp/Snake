using Snake.UseCases;

namespace Snake.Test;

public class ShowFirstLevelRulesAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-FIRST-LEVEL-RULES-001")]
    public void SCN_FIRST_LEVEL_RULES_001_ReturnsOnlyThreeOrderedFirstLevelRules()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();

        ShowFirstLevelRulesResult result = fixture.RulesUseCase.Execute(new ShowFirstLevelRulesQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Collection(
            result.Data.RuleHints,
            rule =>
            {
                Assert.Equal("ORDINARY_FOOD_GROWTH", rule.Code);
                Assert.Equal("普通食物 +1 格", rule.Text);
            },
            rule =>
            {
                Assert.Equal("BOUNDARY_WRAP", rule.Code);
                Assert.Equal("邊界可以穿越", rule.Text);
            },
            rule =>
            {
                Assert.Equal("SELF_COLLISION_DEATH", rule.Code);
                Assert.Equal("撞到自己的身體會死亡", rule.Text);
            });
        Assert.True(fixture.ReadyBoardUseCase.Execute(new ShowFirstLevelReadyBoardQuery()).IsSuccess);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-FIRST-LEVEL-RULES-002")]
    public void SCN_FIRST_LEVEL_RULES_002_ReturnsFailureOutsideReadyPhase()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();
        fixture.StartUseCase.Execute(new StartFirstLevelCommand());

        ShowFirstLevelRulesResult result = fixture.RulesUseCase.Execute(new ShowFirstLevelRulesQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_GAME_PHASE", result.FailureCode);
        Assert.Equal("目前不是第一關待開始階段。", result.FailureMessage);
        Assert.Null(result.Data);
        Assert.Equal(1, fixture.OutputPort.ActivationCount);
    }
}
