using Snake.UseCases;

namespace Snake.Test;

public class StubLevelStatusReadPort : ILevelStatusReadPort
{
    private LevelStatusSourceDTO _status;

    public StubLevelStatusReadPort(LevelStatusSourceDTO status)
    {
        _status = status;
    }

    public int ReadCount { get; private set; }

    public LevelStatusSourceDTO Status
    {
        get => _status;
        set => _status = value;
    }

    public LevelStatusSourceDTO Read()
    {
        ReadCount++;
        return _status;
    }
}
