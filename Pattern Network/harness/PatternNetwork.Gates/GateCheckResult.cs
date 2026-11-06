namespace PatternNetwork.Gates;

public class GateCheckResult
{
    public GateCheckResult(
        string checkId,
        VerificationMode mode,
        VerificationStatus status,
        List<string> ruleRefs,
        List<string> scenarioRefs,
        List<string> evidence,
        List<GateIssue> issues)
    {
        CheckId = checkId;
        Mode = mode;
        Status = status;
        RuleRefs = ruleRefs;
        ScenarioRefs = scenarioRefs;
        Evidence = evidence;
        Issues = issues;
    }

    public bool IsBlockingFailure => Mode == VerificationMode.Blocking
        && (Status == VerificationStatus.Fail || Status == VerificationStatus.Pending);

    public string CheckId { get; set; }

    public VerificationMode Mode { get; set; }

    public VerificationStatus Status { get; set; }

    public List<string> RuleRefs { get; set; }

    public List<string> ScenarioRefs { get; set; }

    public List<string> Evidence { get; set; }

    public List<GateIssue> Issues { get; set; }
}
