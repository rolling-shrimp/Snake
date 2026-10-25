using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class GameplayReadPortTests
{
    [Fact]
    public void Read_ReturnsCompletePresentationSnapshotWithoutEntities()
    {
        SnakeGameSession session = new GameTestSessionFactory().CreatePlaying(LevelNumber.Third);
        session.DoorAndMaze.PrepareThirdLevel(new DoorId(Guid.NewGuid()));
        session.Food.SpawnOrdinary(new FoodItemId(Guid.NewGuid()), new GridPosition(2, 2));
        session.Food.SpawnSpecial(new FoodItemId(Guid.NewGuid()), new GridPosition(3, 2));
        session.Shield.Spawn(new ShieldItemId(Guid.NewGuid()), new GridPosition(4, 2));
        session.Fire.Spawn(new FireItemId(Guid.NewGuid()), new GridPosition(6, 2));
        session.Bomb.Spawn(new BombItemId(Guid.NewGuid()), new GridPosition(7, 2));
        IGameplayReadPort readPort = session;

        GameplayReadModel snapshot = readPort.ReadGameplay();

        Assert.Equal(GameplayPhase.Playing, snapshot.Phase);
        Assert.Equal(3, snapshot.LevelNumber);
        Assert.Equal(10, snapshot.BoardRows);
        Assert.Equal(10, snapshot.BoardColumns);
        Assert.Equal(3, snapshot.SnakeCells.Count);
        Assert.Equal(5, snapshot.Objects.Count);
        Assert.NotEmpty(snapshot.WallCells);
        Assert.Single(snapshot.Doors);
        Assert.Equal(3, snapshot.CurrentLength);
        Assert.Equal(50, snapshot.TargetLength);
        Assert.Equal(6, snapshot.ProgressPercent);
        Assert.Equal(0, snapshot.ShieldRemainingCharges);
        Assert.True(snapshot.IsMovementActive);
        Assert.Equal(250, snapshot.MovementIntervalMilliseconds);
    }
}
