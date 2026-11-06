namespace PatternNetwork.Gates;

public class SpecificationFieldDefinition
{
    public SpecificationFieldDefinition(
        string name,
        bool isRequired,
        SpecificationFieldNodeType nodeType)
    {
        Name = name;
        IsRequired = isRequired;
        NodeType = nodeType;
    }

    public string Name { get; set; }

    public bool IsRequired { get; set; }

    public SpecificationFieldNodeType NodeType { get; set; }
}
