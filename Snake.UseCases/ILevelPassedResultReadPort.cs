namespace Snake.UseCases;

public interface ILevelPassedResultReadPort
{
    bool IsAvailable { get; }
    LevelPassedResultRspDTO Read();
}
