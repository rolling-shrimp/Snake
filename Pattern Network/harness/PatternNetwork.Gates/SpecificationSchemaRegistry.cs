namespace PatternNetwork.Gates;

public class SpecificationSchemaRegistry
{
    private Dictionary<string, SpecificationSchemaDefinition> _schemasByName;

    public SpecificationSchemaRegistry()
    {
        _schemasByName = new Dictionary<string, SpecificationSchemaDefinition>(StringComparer.Ordinal)
        {
            ["problem_frame"] = CreateDefinition(
                "problem_frame",
                SpecificationKind.ProblemFrame,
                new List<SpecificationFieldDefinition>
                {
                    CreateField("frame", true, SpecificationFieldNodeType.Mapping),
                    CreateField("problem_domains", false, SpecificationFieldNodeType.Sequence),
                    CreateField("shared_phenomena", false, SpecificationFieldNodeType.Sequence),
                    CreateField("world_facts", false, SpecificationFieldNodeType.Sequence),
                    CreateField("frame_concerns", false, SpecificationFieldNodeType.Sequence)
                }),
            ["aggregate_contract"] = CreateDefinition(
                "aggregate_contract",
                SpecificationKind.AggregateContract,
                new List<SpecificationFieldDefinition>
                {
                    CreateField("aggregate", true, SpecificationFieldNodeType.Scalar),
                    CreateField("state", false, SpecificationFieldNodeType.Sequence),
                    CreateField("value_objects", false, SpecificationFieldNodeType.Sequence),
                    CreateField("enums", false, SpecificationFieldNodeType.Sequence),
                    CreateField("relations", false, SpecificationFieldNodeType.Sequence),
                    CreateField("invariants", false, SpecificationFieldNodeType.Sequence),
                    CreateField("behaviors", false, SpecificationFieldNodeType.Sequence)
                }),
            ["workflow_contract"] = CreateDefinition(
                "workflow_contract",
                SpecificationKind.WorkflowContract,
                new List<SpecificationFieldDefinition>
                {
                    CreateField("workflow", true, SpecificationFieldNodeType.Scalar),
                    CreateField("frame_ref", false, SpecificationFieldNodeType.Scalar),
                    CreateField("input", false, SpecificationFieldNodeType.Mapping),
                    CreateField("output", false, SpecificationFieldNodeType.Mapping),
                    CreateField("domain_refs", false, SpecificationFieldNodeType.Sequence),
                    CreateField("preconditions", false, SpecificationFieldNodeType.Sequence),
                    CreateField("decisions", false, SpecificationFieldNodeType.Sequence),
                    CreateField("steps", false, SpecificationFieldNodeType.Sequence),
                    CreateField("branches", false, SpecificationFieldNodeType.Sequence),
                    CreateField("stop_conditions", false, SpecificationFieldNodeType.Sequence),
                    CreateField("commit_policy", false, SpecificationFieldNodeType.Mapping),
                    CreateField("postconditions", false, SpecificationFieldNodeType.Sequence),
                    CreateField("failures", false, SpecificationFieldNodeType.Sequence),
                    CreateField("no_op", false, SpecificationFieldNodeType.Sequence)
                }),
            ["technical_contract"] = CreateDefinition(
                "technical_contract",
                SpecificationKind.TechnicalContract,
                new List<SpecificationFieldDefinition>
                {
                    CreateField("operation", true, SpecificationFieldNodeType.Scalar),
                    CreateField("workflow_ref", true, SpecificationFieldNodeType.Scalar),
                    CreateField("technical_ref", false, SpecificationFieldNodeType.Scalar),
                    CreateField("input", false, SpecificationFieldNodeType.Mapping),
                    CreateField("output", false, SpecificationFieldNodeType.Mapping),
                    CreateField("types", false, SpecificationFieldNodeType.Sequence),
                    CreateField("ports", false, SpecificationFieldNodeType.Sequence),
                    CreateField("error_mappings", false, SpecificationFieldNodeType.Sequence),
                    CreateField("domain_mappings", false, SpecificationFieldNodeType.Sequence),
                    CreateField("implementation_binding", false, SpecificationFieldNodeType.Mapping)
                }),
            ["acceptance_contract"] = CreateDefinition(
                "acceptance_contract",
                SpecificationKind.AcceptanceContract,
                new List<SpecificationFieldDefinition>
                {
                    CreateField("contract_ref", true, SpecificationFieldNodeType.Scalar),
                    CreateField("scenarios", true, SpecificationFieldNodeType.Sequence),
                    CreateField("evidence_requirements", false, SpecificationFieldNodeType.Sequence)
                }),
            ["journey_contract"] = CreateDefinition(
                "journey_contract",
                SpecificationKind.JourneyContract,
                new List<SpecificationFieldDefinition>
                {
                    CreateField("journey", true, SpecificationFieldNodeType.Scalar),
                    CreateField("states", false, SpecificationFieldNodeType.Sequence),
                    CreateField("state_refs", false, SpecificationFieldNodeType.Sequence),
                    CreateField("transitions", false, SpecificationFieldNodeType.Sequence),
                    CreateField("workflow_refs", false, SpecificationFieldNodeType.Sequence),
                    CreateField("terminal_states", false, SpecificationFieldNodeType.Sequence)
                }),
            ["presentation_state"] = CreateDefinition(
                "presentation_state",
                SpecificationKind.PresentationState,
                new List<SpecificationFieldDefinition>
                {
                    CreateField("screen", true, SpecificationFieldNodeType.Scalar),
                    CreateField("grain", false, SpecificationFieldNodeType.Scalar),
                    CreateField("relations", false, SpecificationFieldNodeType.Sequence),
                    CreateField("visibility", false, SpecificationFieldNodeType.Mapping),
                    CreateField("transitions", false, SpecificationFieldNodeType.Sequence),
                    CreateField("variants", false, SpecificationFieldNodeType.Sequence)
                }),
            ["presentation_workflow"] = CreateDefinition(
                "presentation_workflow",
                SpecificationKind.PresentationWorkflow,
                new List<SpecificationFieldDefinition>
                {
                    CreateField("workflow", true, SpecificationFieldNodeType.Scalar),
                    CreateField("workflow_ref", false, SpecificationFieldNodeType.Scalar),
                    CreateField("input", false, SpecificationFieldNodeType.Mapping),
                    CreateField("output", false, SpecificationFieldNodeType.Mapping),
                    CreateField("render_policy", false, SpecificationFieldNodeType.Mapping),
                    CreateField("runtime_policy", false, SpecificationFieldNodeType.Mapping),
                    CreateField("state_refs", false, SpecificationFieldNodeType.Sequence),
                    CreateField("technical_ref", false, SpecificationFieldNodeType.Scalar),
                    CreateField("acceptance_ref", false, SpecificationFieldNodeType.Scalar)
                }),
            ["spec_catalog"] = CreateDefinition(
                "spec_catalog",
                SpecificationKind.SpecificationCatalog,
                new List<SpecificationFieldDefinition>
                {
                    CreateField("catalog", true, SpecificationFieldNodeType.Scalar),
                    CreateField("frames", false, SpecificationFieldNodeType.Sequence),
                    CreateField("aggregates", false, SpecificationFieldNodeType.Sequence),
                    CreateField("workflows", false, SpecificationFieldNodeType.Sequence),
                    CreateField("technical_contracts", false, SpecificationFieldNodeType.Sequence),
                    CreateField("acceptance_contracts", false, SpecificationFieldNodeType.Sequence),
                    CreateField("journeys", false, SpecificationFieldNodeType.Sequence),
                    CreateField("presentation_states", false, SpecificationFieldNodeType.Sequence),
                    CreateField("presentation_workflows", false, SpecificationFieldNodeType.Sequence),
                    CreateField("migrations", false, SpecificationFieldNodeType.Sequence)
                }),
            ["migration_map"] = CreateDefinition(
                "migration_map",
                SpecificationKind.MigrationMap,
                new List<SpecificationFieldDefinition>
                {
                    CreateField("migration", true, SpecificationFieldNodeType.Scalar),
                    CreateField("entries", true, SpecificationFieldNodeType.Sequence)
                })
        };
    }

    public bool TryGet(string yamlName, out SpecificationSchemaDefinition? definition)
    {
        return _schemasByName.TryGetValue(yamlName, out definition);
    }

    private SpecificationSchemaDefinition CreateDefinition(
        string yamlName,
        SpecificationKind specificationKind,
        List<SpecificationFieldDefinition> kindFields)
    {
        List<SpecificationFieldDefinition> fields = new List<SpecificationFieldDefinition>
        {
            CreateField("schema_version", true, SpecificationFieldNodeType.Scalar),
            CreateField("spec_kind", true, SpecificationFieldNodeType.Scalar),
            CreateField("id", true, SpecificationFieldNodeType.Scalar)
        };
        fields.AddRange(kindFields);
        return new SpecificationSchemaDefinition(yamlName, specificationKind, fields);
    }

    private static SpecificationFieldDefinition CreateField(
        string name,
        bool isRequired,
        SpecificationFieldNodeType nodeType)
    {
        return new SpecificationFieldDefinition(name, isRequired, nodeType);
    }
}
