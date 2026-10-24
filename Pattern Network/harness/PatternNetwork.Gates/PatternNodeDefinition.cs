namespace PatternNetwork.Gates;

public class PatternNodeDefinition
{
    public PatternNodeDefinition()
    {
        AllowedProjectReferences = new List<string>();
        Checks = new List<GateCheckDefinition>();
    }

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DirectoryPath { get; set; } = string.Empty;

    public string TargetFramework { get; set; } = string.Empty;

    public bool UseWindowsForms { get; set; }

    public List<string> AllowedProjectReferences { get; set; }

    public List<GateCheckDefinition> Checks { get; set; }
}
