namespace Snake.UseCases;

public interface IGameCompletedResultReadPort
{
    bool IsAvailable { get; }
    GameCompletedResultRspDTO Read();
}
