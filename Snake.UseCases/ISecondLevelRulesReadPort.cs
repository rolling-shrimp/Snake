namespace Snake.UseCases;

public interface ISecondLevelRulesReadPort
{
    bool IsAvailable { get; }
    SecondLevelRulesRspDTO Read();
}
