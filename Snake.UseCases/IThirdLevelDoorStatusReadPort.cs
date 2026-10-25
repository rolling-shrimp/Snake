namespace Snake.UseCases;

public interface IThirdLevelDoorStatusReadPort
{
    bool IsAvailable { get; }
    ThirdLevelDoorStatusRspDTO Read();
}
