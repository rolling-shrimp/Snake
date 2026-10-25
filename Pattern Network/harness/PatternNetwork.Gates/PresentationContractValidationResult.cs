namespace PatternNetwork.Gates;

public class PresentationContractValidationResult
{
    public PresentationContractValidationResult(List<GateIssue> issues, bool hasConfigurationError)
    {
        Issues = issues;
        HasConfigurationError = hasConfigurationError;
    }

    public bool IsValid => !HasConfigurationError && Issues.Count == 0;

    public bool HasConfigurationError { get; set; }

    public List<GateIssue> Issues { get; set; }
}
