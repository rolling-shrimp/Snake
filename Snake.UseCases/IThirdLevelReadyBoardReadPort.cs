namespace Snake.UseCases;

public interface IThirdLevelReadyBoardReadPort
{
    bool IsAvailable { get; }
    ThirdLevelReadyBoardRspDTO Read();
}
