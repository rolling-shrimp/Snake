namespace Snake.UseCases;

public interface IGameSnapshotStore
{
    VersionedSnakeGameSnapshot Load();

    bool TryCommit(long expectedVersion, SnakeGameSnapshot snapshot);
}
