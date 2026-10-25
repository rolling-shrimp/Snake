namespace Snake.UseCases;

public class UpdateLevelStatusReadModelCommand
{
    public UpdateLevelStatusReadModelCommand(string sourceEventId, long sourceVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceEventId);

        SourceEventId = sourceEventId;
        SourceVersion = sourceVersion;
    }

    public string SourceEventId { get; }
    public long SourceVersion { get; }
}
