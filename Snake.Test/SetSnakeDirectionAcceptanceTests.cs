using Snake.UseCases;

namespace Snake.Test;

public class SetSnakeDirectionAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-SET-SNAKE-DIRECTION-001")]
    public void SCN_SET_SNAKE_DIRECTION_001_QueuesUpWithoutChangingBodyOrLength()
    {
        FirstLevelTestFixture fixture = new FirstLevelTestFixture();
        fixture.StartUseCase.Execute(new StartFirstLevelCommand());
        SetSnakeDirectionUseCase useCase = new SetSnakeDirectionUseCase(fixture.Session);
        List<Snake.Entities.GridPosition> bodyBefore = fixture.Session.Snake.BodyCells;
        int lengthBefore = fixture.Session.Snake.Length;
        Snake.Entities.Direction currentDirectionBefore = fixture.Session.Snake.CurrentDirection;

        SetSnakeDirectionResult result = useCase.Execute(new SetSnakeDirectionCommand(Direction.Up));

        Assert.True(result.IsSuccess);
        Assert.Null(result.FailureCode);
        Assert.Null(result.FailureMessage);
        Assert.Equal(Snake.Entities.Direction.Up, fixture.Session.Snake.QueuedDirection);
        Assert.Equal(bodyBefore, fixture.Session.Snake.BodyCells);
        Assert.Equal(lengthBefore, fixture.Session.Snake.Length);
        Assert.Equal(currentDirectionBefore, fixture.Session.Snake.CurrentDirection);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-SET-SNAKE-DIRECTION-002")]
    public void SCN_SET_SNAKE_DIRECTION_002_IgnoresReadyPhaseAndDirectReverseInput()
    {
        SnakeGameSession readySession = new SnakeGameSession();
        SetSnakeDirectionUseCase readyUseCase = new SetSnakeDirectionUseCase(readySession);
        List<Snake.Entities.GridPosition> readyBodyBefore = readySession.Snake.BodyCells;

        SetSnakeDirectionResult readyResult = readyUseCase.Execute(new SetSnakeDirectionCommand(Direction.Up));

        Assert.True(readyResult.IsSuccess);
        Assert.Null(readySession.Snake.QueuedDirection);
        Assert.Equal(readyBodyBefore, readySession.Snake.BodyCells);

        FirstLevelTestFixture playingFixture = new FirstLevelTestFixture();
        playingFixture.StartUseCase.Execute(new StartFirstLevelCommand());
        SetSnakeDirectionUseCase playingUseCase = new SetSnakeDirectionUseCase(playingFixture.Session);
        List<Snake.Entities.GridPosition> playingBodyBefore = playingFixture.Session.Snake.BodyCells;

        SetSnakeDirectionResult reverseResult = playingUseCase.Execute(new SetSnakeDirectionCommand(Direction.Left));

        Assert.True(reverseResult.IsSuccess);
        Assert.Null(playingFixture.Session.Snake.QueuedDirection);
        Assert.Equal(playingBodyBefore, playingFixture.Session.Snake.BodyCells);
    }
}
