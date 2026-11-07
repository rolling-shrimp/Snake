namespace PatternNetwork.Gates;

public class VerificationProjectDefinition
{
    public VerificationProjectDefinition()
    {
        RequiredProjectReferences = new List<string>();
    }

    public string Name { get; set; } = string.Empty;

    public string SolutionFolder { get; set; } = string.Empty;

    public string TargetFramework { get; set; } = string.Empty;

    public bool UseWindowsForms { get; set; }

    public List<string> RequiredProjectReferences { get; set; }
}
