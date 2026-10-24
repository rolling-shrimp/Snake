namespace PatternNetwork.Gates;

public class GateCheckDefinition
{
    public GateCheckDefinition()
    {
        RuleReferences = new List<string>();
        ScalarParameters = new Dictionary<string, string>(StringComparer.Ordinal);
        ListParameters = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    }

    public string Id { get; set; } = string.Empty;

    public string Checker { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public List<string> RuleReferences { get; set; }

    public Dictionary<string, string> ScalarParameters { get; set; }

    public Dictionary<string, List<string>> ListParameters { get; set; }
}
