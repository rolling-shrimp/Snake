namespace Snake.UseCases;

public interface IGameOverResultReadPort
{
    bool IsAvailable { get; }
    GameOverResultRspDTO Read();
}
