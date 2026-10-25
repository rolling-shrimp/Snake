namespace Snake.UseCases;

public class FirstLevelRulesProjection
{
    public FirstLevelRulesRspDTO Project(FirstLevelReadModel readModel)
    {
        if (readModel.OrdinaryFoodLengthDelta != 1 || !readModel.BoundaryWraps || !readModel.SelfCollisionIsFatal)
        {
            throw new InvalidOperationException("第一關規則與 Domain 設定不一致。");
        }

        List<RuleHintDTO> ruleHints = new List<RuleHintDTO>
        {
            new RuleHintDTO("ORDINARY_FOOD_GROWTH", "普通食物 +1 格"),
            new RuleHintDTO("BOUNDARY_WRAP", "邊界可以穿越"),
            new RuleHintDTO("SELF_COLLISION_DEATH", "撞到自己的身體會死亡")
        };
        return new FirstLevelRulesRspDTO(ruleHints);
    }
}
