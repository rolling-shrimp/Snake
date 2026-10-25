namespace Snake.UseCases;

public class FirstLevelStatusProjection
{
    public FirstLevelStatusRspDTO Project(FirstLevelReadModel readModel)
    {
        string levelName = readModel.LevelNumber == 1
            ? "第一關"
            : throw new InvalidOperationException("目前關卡不是第一關。");
        return new FirstLevelStatusRspDTO(
            levelName,
            readModel.CurrentLength,
            readModel.TargetLength,
            readModel.ProgressPercent);
    }
}
