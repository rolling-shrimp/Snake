namespace Snake.UseCases;

public class UpdateLevelStatusReadModelUseCase
{
    private ILevelStatusReadPort _readPort;
    private HashSet<string> _processedEventIds;
    private object _syncRoot;
    private LevelStatusReadModel? _current;

    public UpdateLevelStatusReadModelUseCase(ILevelStatusReadPort readPort)
    {
        ArgumentNullException.ThrowIfNull(readPort);

        _readPort = readPort;
        _processedEventIds = new HashSet<string>(StringComparer.Ordinal);
        _syncRoot = new object();
    }

    public LevelStatusReadModel? Current => _current;

    public UpdateLevelStatusReadModelResult Execute(UpdateLevelStatusReadModelCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        lock (_syncRoot)
        {
            if (_processedEventIds.Contains(command.SourceEventId))
            {
                return Succeed();
            }

            if (_current is not null && command.SourceVersion <= _current.SourceVersion)
            {
                return Succeed();
            }

            LevelStatusSourceDTO source = _readPort.Read();
            LevelStatusReadModel projectedStatus = Project(source, command.SourceVersion);

            _processedEventIds.Add(command.SourceEventId);
            _current = projectedStatus;

            return Succeed();
        }
    }

    private LevelStatusReadModel Project(LevelStatusSourceDTO source, long sourceVersion)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source.TargetLength <= 0)
        {
            throw new InvalidOperationException("關卡目標長度必須大於零。");
        }

        string levelName = source.LevelNumber switch
        {
            1 => "第一關",
            2 => "第二關",
            3 => "第三關",
            _ => throw new InvalidOperationException($"不支援的關卡編號：{source.LevelNumber}。")
        };

        int progressPercent = Math.Clamp(
            source.CurrentLength * 100 / source.TargetLength,
            0,
            100);

        return new LevelStatusReadModel(
            levelName,
            source.CurrentLength,
            source.TargetLength,
            progressPercent,
            source.ShieldRemainingCharges,
            sourceVersion);
    }

    private UpdateLevelStatusReadModelResult Succeed()
    {
        return new UpdateLevelStatusReadModelResult(true, null, null);
    }
}
