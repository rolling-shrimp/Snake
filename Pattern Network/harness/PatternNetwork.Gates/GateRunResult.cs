namespace PatternNetwork.Gates;

public class GateRunResult
{
    public GateRunResult(List<GateIssue> issues, bool hasConfigurationError)
    {
        Issues = issues;
        HasConfigurationError = hasConfigurationError;
    }

    public bool IsSuccessful => !HasConfigurationError && Issues.Count == 0;

    public bool HasConfigurationError { get; set; }

    public List<GateIssue> Issues { get; set; }
}
