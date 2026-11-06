namespace PatternNetwork.Gates;

public class SpecificationValidationResult
{
    public SpecificationValidationResult(
        SpecificationInventory inventory,
        List<GateIssue> issues,
        bool hasConfigurationError)
    {
        Inventory = inventory;
        Issues = issues;
        HasConfigurationError = hasConfigurationError;
    }

    public bool IsValid => !HasConfigurationError && Issues.Count == 0;

    public SpecificationInventory Inventory { get; set; }

    public List<GateIssue> Issues { get; set; }

    public bool HasConfigurationError { get; set; }
}
