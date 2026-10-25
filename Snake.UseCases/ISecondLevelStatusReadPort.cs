namespace Snake.UseCases;

public interface ISecondLevelStatusReadPort
{
    bool IsAvailable { get; }
    SecondLevelStatusRspDTO Read();
}
