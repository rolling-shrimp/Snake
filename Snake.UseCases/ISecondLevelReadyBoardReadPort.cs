namespace Snake.UseCases;

public interface ISecondLevelReadyBoardReadPort
{
    bool IsAvailable { get; }
    SecondLevelReadyBoardRspDTO Read();
}
