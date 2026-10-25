using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class HandleBombContactAcceptanceTests
{
    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-BOMB-CONTACT-001")]
    public void SCN_HANDLE_BOMB_CONTACT_001_RemovesBombAndPublishesCollisionWithoutEndingGame()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third);
        BombItemId bombItemId = new BombItemId(Guid.NewGuid());
        session.Bomb.Spawn(bombItemId, session.Snake.BodyCells[0]);
        HandleBombContactUseCase useCase = new HandleBombContactUseCase(session);

        HandleBombContactResult result = useCase.Execute(
            new HandleBombContactCommand(bombItemId.Value.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Null(session.Bomb.ActiveItem);
        Assert.Single(session.PublishedFacts.OfType<BombCollisionDomainEvent>());
        Assert.Equal(GamePhase.Playing, session.Game.Phase);
    }

    [Fact]
    [Trait("ScenarioId", "SCN-HANDLE-BOMB-CONTACT-002")]
    public void SCN_HANDLE_BOMB_CONTACT_002_IgnoresWrongLevelAndInactiveBomb()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Second);
        BombItemId bombItemId = new BombItemId(Guid.NewGuid());
        session.Bomb.Spawn(bombItemId, session.Snake.BodyCells[0]);
        HandleBombContactUseCase useCase = new HandleBombContactUseCase(session);

        HandleBombContactResult wrongLevelResult = useCase.Execute(
            new HandleBombContactCommand(bombItemId.Value.ToString()));
        HandleBombContactResult inactiveResult = useCase.Execute(
            new HandleBombContactCommand(Guid.NewGuid().ToString()));

        Assert.True(wrongLevelResult.IsSuccess);
        Assert.True(inactiveResult.IsSuccess);
        Assert.NotNull(session.Bomb.ActiveItem);
        Assert.Empty(session.PublishedFacts);
    }
}
