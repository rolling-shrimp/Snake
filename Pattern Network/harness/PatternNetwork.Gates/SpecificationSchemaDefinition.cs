namespace PatternNetwork.Gates;

public class SpecificationSchemaDefinition
{
    public SpecificationSchemaDefinition(
        string yamlName,
        SpecificationKind specificationKind,
        List<SpecificationFieldDefinition> fields)
    {
        YamlName = yamlName;
        SpecificationKind = specificationKind;
        Fields = fields;
    }

    public string YamlName { get; set; }

    public SpecificationKind SpecificationKind { get; set; }

    public List<SpecificationFieldDefinition> Fields { get; set; }
}
