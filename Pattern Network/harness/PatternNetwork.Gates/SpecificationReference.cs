namespace PatternNetwork.Gates;

public class SpecificationReference
{
    public SpecificationReference(
        string fieldName,
        string targetId,
        SpecificationKind? expectedKind,
        int? line)
    {
        FieldName = fieldName;
        TargetId = targetId;
        ExpectedKind = expectedKind;
        Line = line;
    }

    public string FieldName { get; set; }

    public string TargetId { get; set; }

    public SpecificationKind? ExpectedKind { get; set; }

    public int? Line { get; set; }
}
