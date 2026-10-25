namespace Snake.UseCases;

public class ThirdLevelRulesRspDTO
{
    private List<RuleHintDTO> _ruleHints;

    public ThirdLevelRulesRspDTO(List<RuleHintDTO> ruleHints)
    {
        _ruleHints = new List<RuleHintDTO>(ruleHints);
    }

    public List<RuleHintDTO> RuleHints => new List<RuleHintDTO>(_ruleHints);
}
