namespace Snake.UseCases;

public class SecondLevelRulesRspDTO
{
    private List<RuleHintDTO> _ruleHints;

    public SecondLevelRulesRspDTO(List<RuleHintDTO> ruleHints)
    {
        _ruleHints = new List<RuleHintDTO>(ruleHints);
    }

    public List<RuleHintDTO> RuleHints => new List<RuleHintDTO>(_ruleHints);
}
