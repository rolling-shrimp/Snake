namespace Snake.UseCases;

public interface IThirdLevelStatusReadPort
{
    bool IsAvailable { get; }
    ThirdLevelStatusRspDTO Read();
}
