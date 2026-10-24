namespace PatternNetwork.Gates;

public class PatternNetworkValidationResult
{
    public PatternNetworkValidationResult(PatternNetworkDefinition definition, List<GateIssue> issues)
    {
        Definition = definition;
        Issues = issues;
    }

    public bool IsValid => Issues.Count == 0;

    public PatternNetworkDefinition Definition { get; set; }

    public List<GateIssue> Issues { get; set; }
}
