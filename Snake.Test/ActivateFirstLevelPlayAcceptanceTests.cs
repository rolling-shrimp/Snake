using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class ActivateFirstLevelPlayAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-ACTIVATE-FIRST-LEVEL-001")]
    public void SCN_ACTIVATE_FIRST_LEVEL_001_CreatesCompletePlayingStateInRequiredOrder()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();
        ShowFirstLevelReadyBoardResult ready = fixture.ReadyBoardUseCase.Execute(new ShowFirstLevelReadyBoardQuery());
        StartFirstLevelAcceptedDomainEvent domainEvent = new StartFirstLevelAcceptedDomainEvent();

        ActivateFirstLevelPlayResult result = fixture.Reactor.React(domainEvent);

        Assert.True(result.IsSuccess);
        Assert.Equal(new List<string> { "hide_start_button", "spawn_ordinary_food", "start_movement" }, fixture.OutputPort.Operations);
        Assert.NotNull(fixture.OutputPort.Food);
        Assert.Equal(GameObjectType.Strawberry, fixture.OutputPort.Food.ObjectType);
        Assert.InRange(fixture.OutputPort.Food.Position.X, 0, 9);
        Assert.InRange(fixture.OutputPort.Food.Position.Y, 0, 9);
        Assert.DoesNotContain(ready.Data!.SnakeCells, cell => cell.X == fixture.OutputPort.Food.Position.X && cell.Y == fixture.OutputPort.Food.Position.Y);
        Assert.NotNull(fixture.OutputPort.Movement);
        Assert.True(fixture.OutputPort.Movement.IsActive);
        Assert.Equal(250, fixture.OutputPort.Movement.IntervalMilliseconds);
        Assert.Null(fixture.OutputPort.Movement.LevelTimeLimitMilliseconds);
        Assert.False(fixture.ReadyBoardUseCase.Execute(new ShowFirstLevelReadyBoardQuery()).IsSuccess);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-ACTIVATE-FIRST-LEVEL-002")]
    public void SCN_ACTIVATE_FIRST_LEVEL_002_IgnoresUnknownEventSource()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();

        ActivateFirstLevelPlayResult result = fixture.Reactor.React(new UnknownDomainEvent());

        Assert.True(result.IsSuccess);
        Assert.Empty(fixture.OutputPort.Operations);
        Assert.True(fixture.ReadyBoardUseCase.Execute(new ShowFirstLevelReadyBoardQuery()).IsSuccess);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-ACTIVATE-FIRST-LEVEL-003")]
    public void SCN_ACTIVATE_FIRST_LEVEL_003_IgnoresDuplicateAcceptedEvent()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();
        StartFirstLevelAcceptedDomainEvent domainEvent = new StartFirstLevelAcceptedDomainEvent();

        ActivateFirstLevelPlayResult firstResult = fixture.Reactor.React(domainEvent);
        ActivateFirstLevelPlayResult secondResult = fixture.Reactor.React(domainEvent);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Equal(1, fixture.OutputPort.ActivationCount);
        Assert.Equal(3, fixture.OutputPort.Operations.Count);
    }
}
