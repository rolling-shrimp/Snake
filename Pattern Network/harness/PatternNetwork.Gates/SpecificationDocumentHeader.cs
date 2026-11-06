namespace PatternNetwork.Gates;

public class SpecificationDocumentHeader
{
    public SpecificationDocumentHeader(int schemaVersion, SpecificationKind specificationKind, string id)
    {
        SchemaVersion = schemaVersion;
        SpecificationKind = specificationKind;
        Id = id;
    }

    public int SchemaVersion { get; set; }

    public SpecificationKind SpecificationKind { get; set; }

    public string Id { get; set; }
}
