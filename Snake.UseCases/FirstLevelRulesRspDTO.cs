namespace Snake.UseCases;

public class FirstLevelRulesRspDTO
{
    private List<RuleHintDTO> _ruleHints;

    public FirstLevelRulesRspDTO(List<RuleHintDTO> ruleHints)
    {
        _ruleHints = new List<RuleHintDTO>(ruleHints);
    }

    public List<RuleHintDTO> RuleHints => new List<RuleHintDTO>(_ruleHints);
}
