using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class EvaluateLevelCompletionAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-EVALUATE-LEVEL-COMPLETION-001")]
    public void SCN_EVALUATE_LEVEL_COMPLETION_001_PublishesFirstLevelPassedOnceAtTwenty()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.First, 20);
        EvaluateLevelCompletionUseCase useCase = new EvaluateLevelCompletionUseCase(session);

        EvaluateLevelCompletionResult firstResult = useCase.Execute(new EvaluateLevelCompletionCommand());
        EvaluateLevelCompletionResult duplicateResult = useCase.Execute(new EvaluateLevelCompletionCommand());

        Assert.True(firstResult.IsSuccess);
        Assert.True(duplicateResult.IsSuccess);
        LevelPassedDomainEvent levelPassed = Assert.IsType<LevelPassedDomainEvent>(Assert.Single(session.PublishedFacts));
        Assert.Equal(LevelNumber.First, levelPassed.LevelNumber);
        Assert.Equal(20, levelPassed.ActualLength);
        Assert.Equal(20, session.Snake.Length);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-EVALUATE-LEVEL-COMPLETION-002")]
    public void SCN_EVALUATE_LEVEL_COMPLETION_002_PreservesThirdLevelActualLengthAboveTarget()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third, 51);
        EvaluateLevelCompletionUseCase useCase = new EvaluateLevelCompletionUseCase(session);

        EvaluateLevelCompletionResult result = useCase.Execute(new EvaluateLevelCompletionCommand());

        Assert.True(result.IsSuccess);
        LevelPassedDomainEvent levelPassed = Assert.IsType<LevelPassedDomainEvent>(Assert.Single(session.PublishedFacts));
        Assert.Equal(51, levelPassed.ActualLength);
        Assert.Equal(51, session.Snake.Length);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-EVALUATE-LEVEL-COMPLETION-003")]
    public void SCN_EVALUATE_LEVEL_COMPLETION_003_DoesNothingBelowTarget()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second, 19);
        EvaluateLevelCompletionUseCase useCase = new EvaluateLevelCompletionUseCase(session);

        EvaluateLevelCompletionResult result = useCase.Execute(new EvaluateLevelCompletionCommand());

        Assert.True(result.IsSuccess);
        Assert.Empty(session.PublishedFacts);
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
    }
}
