namespace PatternNetwork.Gates;

public class GateIssue
{
    public GateIssue(string ruleId, string node, string file, int? line, string message, string severity)
    {
        RuleId = ruleId;
        Node = node;
        File = file;
        Line = line;
        Message = message;
        Severity = severity;
    }

    public string RuleId { get; set; }

    public string Node { get; set; }

    public string File { get; set; }

    public int? Line { get; set; }

    public string Message { get; set; }

    public string Severity { get; set; }
}
