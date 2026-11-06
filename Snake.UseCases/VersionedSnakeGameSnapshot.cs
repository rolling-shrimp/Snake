namespace Snake.UseCases;

public class VersionedSnakeGameSnapshot
{
    public VersionedSnakeGameSnapshot(long version, SnakeGameSnapshot snapshot)
    {
        Version = version;
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public long Version { get; }
    public SnakeGameSnapshot Snapshot { get; }
}
