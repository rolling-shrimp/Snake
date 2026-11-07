namespace PatternNetwork.Gates;

public class PatternNetworkDefinition
{
    public PatternNetworkDefinition(
        string networkPath,
        List<PatternNodeDefinition> patternNodes,
        VerificationProjectDefinition verificationProject)
    {
        NetworkPath = networkPath;
        PatternNodes = patternNodes;
        VerificationProject = verificationProject;
    }

    public string NetworkPath { get; set; }

    public List<PatternNodeDefinition> PatternNodes { get; set; }

    public VerificationProjectDefinition VerificationProject { get; set; }
}
