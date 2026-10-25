namespace Snake.UseCases;

public interface IThirdLevelRulesReadPort
{
    bool IsAvailable { get; }
    ThirdLevelRulesRspDTO Read();
}
