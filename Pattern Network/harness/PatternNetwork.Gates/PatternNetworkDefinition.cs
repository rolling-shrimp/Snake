namespace PatternNetwork.Gates;

public class PatternNetworkDefinition
{
    public PatternNetworkDefinition(string networkPath, List<PatternNodeDefinition> patternNodes)
    {
        NetworkPath = networkPath;
        PatternNodes = patternNodes;
    }

    public string NetworkPath { get; set; }

    public List<PatternNodeDefinition> PatternNodes { get; set; }
}
