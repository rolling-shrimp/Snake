namespace PatternNetwork.Gates;

public class MigrationMapValidationResult
{
    public MigrationMapValidationResult(List<GateIssue> issues, bool hasConfigurationError)
    {
        Issues = issues;
        HasConfigurationError = hasConfigurationError;
    }

    public bool IsValid => !HasConfigurationError && Issues.Count == 0;

    public List<GateIssue> Issues { get; set; }

    public bool HasConfigurationError { get; set; }
}
