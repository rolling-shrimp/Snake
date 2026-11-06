namespace PatternNetwork.Gates;

public class GateRunResult
{
    public GateRunResult(List<GateIssue> issues, bool hasConfigurationError)
    {
        Issues = issues;
        HasConfigurationError = hasConfigurationError;
        Checks = new List<GateCheckResult>
        {
            new GateCheckResult(
                "legacy-gate",
                VerificationMode.Blocking,
                issues.Count == 0 ? VerificationStatus.Pass : VerificationStatus.Fail,
                new List<string>(),
                new List<string>(),
                new List<string>(),
                issues)
        };
    }

    public GateRunResult(List<GateCheckResult> checks, bool hasConfigurationError)
    {
        Checks = checks;
        HasConfigurationError = hasConfigurationError;
        Issues = checks.SelectMany(check => check.Issues).ToList();
    }

    public bool IsSuccessful => !HasConfigurationError && Checks.All(check => !check.IsBlockingFailure);

    public bool HasConfigurationError { get; set; }

    public List<GateIssue> Issues { get; set; }

    public List<GateCheckResult> Checks { get; set; }
}
