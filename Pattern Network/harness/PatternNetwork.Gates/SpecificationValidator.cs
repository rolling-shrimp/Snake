using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace PatternNetwork.Gates;

public class SpecificationValidator
{
    private SpecificationSchemaRegistry _schemaRegistry;

    public SpecificationValidator()
    {
        _schemaRegistry = new SpecificationSchemaRegistry();
    }

    public SpecificationValidationResult Validate(string domainsPath)
    {
        return Validate(domainsPath, new List<string>());
    }

    public SpecificationValidationResult Validate(string domainsPath, List<string> entryPaths)
    {
        List<SpecificationDocument> documents = new List<SpecificationDocument>();
        List<GateIssue> issues = new List<GateIssue>();
        if (!Directory.Exists(domainsPath))
        {
            issues.Add(CreateIssue(
                "V3-SPEC-CONFIG-DOMAINS",
                domainsPath,
                null,
                "找不到 Domains 規格目錄。"));
            return new SpecificationValidationResult(
                new SpecificationInventory(documents),
                issues,
                true);
        }

        string fullDomainsPath = Path.GetFullPath(domainsPath);
        string[] allSpecificationPaths = Directory.GetFiles(fullDomainsPath, "*.yaml", SearchOption.AllDirectories);
        Array.Sort(allSpecificationPaths, StringComparer.Ordinal);
        if (allSpecificationPaths.Length == 0)
        {
            issues.Add(CreateIssue(
                "V3-SPEC-CONFIG-EMPTY",
                domainsPath,
                null,
                "Domains 規格目錄中沒有 YAML 文件。"));
            return new SpecificationValidationResult(
                new SpecificationInventory(documents),
                issues,
                true);
        }

        bool hasConfigurationError = false;
        if (entryPaths.Count == 0)
        {
            foreach (string specificationPath in allSpecificationPaths)
            {
                ValidateDocumentSafely(
                    specificationPath,
                    documents,
                    issues,
                    ref hasConfigurationError);
            }
        }
        else
        {
            List<string> resolvedEntryPaths = ResolveEntryPaths(
                fullDomainsPath,
                entryPaths,
                issues,
                ref hasConfigurationError);
            if (!hasConfigurationError)
            {
                ValidateEntryClosure(
                    allSpecificationPaths,
                    resolvedEntryPaths,
                    documents,
                    issues,
                    ref hasConfigurationError);
            }
        }

        ValidateUniqueIds(documents, issues);
        ValidateReferences(documents, issues);
        return new SpecificationValidationResult(
            new SpecificationInventory(documents),
            issues,
            hasConfigurationError);
    }

    private List<string> ResolveEntryPaths(
        string domainsPath,
        List<string> entryPaths,
        List<GateIssue> issues,
        ref bool hasConfigurationError)
    {
        string fullDomainsPath = Path.GetFullPath(domainsPath);
        List<string> resolvedEntryPaths = new List<string>();
        HashSet<string> uniqueEntryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string entryPath in entryPaths)
        {
            string fullEntryPath;
            try
            {
                fullEntryPath = Path.GetFullPath(
                    Path.IsPathRooted(entryPath)
                        ? entryPath
                        : Path.Combine(fullDomainsPath, entryPath));
            }
            catch (Exception exception)
            {
                issues.Add(CreateIssue(
                    "V3-SPEC-CONFIG-ENTRY",
                    entryPath,
                    null,
                    $"無法解析 entry 規格路徑：{exception.Message}"));
                hasConfigurationError = true;
                continue;
            }

            string relativePath = Path.GetRelativePath(fullDomainsPath, fullEntryPath);
            if (Path.IsPathRooted(relativePath)
                || relativePath == ".."
                || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
            {
                issues.Add(CreateIssue(
                    "V3-SPEC-CONFIG-ENTRY",
                    fullEntryPath,
                    null,
                    "entry 規格必須位於指定的 Domains 目錄內。"));
                hasConfigurationError = true;
                continue;
            }

            if (!File.Exists(fullEntryPath))
            {
                issues.Add(CreateIssue(
                    "V3-SPEC-CONFIG-ENTRY",
                    fullEntryPath,
                    null,
                    "找不到 entry 規格檔案。"));
                hasConfigurationError = true;
                continue;
            }

            if (uniqueEntryPaths.Add(fullEntryPath))
            {
                resolvedEntryPaths.Add(fullEntryPath);
            }
        }

        return resolvedEntryPaths;
    }

    private void ValidateEntryClosure(
        string[] allSpecificationPaths,
        List<string> entryPaths,
        List<SpecificationDocument> documents,
        List<GateIssue> issues,
        ref bool hasConfigurationError)
    {
        Dictionary<string, List<string>> v3PathsById = BuildV3PathIndex(allSpecificationPaths);
        Queue<string> pendingPaths = new Queue<string>(entryPaths);
        HashSet<string> scheduledPaths = new HashSet<string>(entryPaths, StringComparer.OrdinalIgnoreCase);
        while (pendingPaths.Count > 0)
        {
            string specificationPath = pendingPaths.Dequeue();
            int documentCount = documents.Count;
            ValidateDocumentSafely(
                specificationPath,
                documents,
                issues,
                ref hasConfigurationError);
            if (documents.Count == documentCount)
            {
                continue;
            }

            SpecificationDocument document = documents[^1];
            foreach (SpecificationReference reference in document.References)
            {
                if (!v3PathsById.TryGetValue(reference.TargetId, out List<string>? referencePaths))
                {
                    continue;
                }

                foreach (string referencePath in referencePaths)
                {
                    if (scheduledPaths.Add(referencePath))
                    {
                        pendingPaths.Enqueue(referencePath);
                    }
                }
            }
        }
    }

    private Dictionary<string, List<string>> BuildV3PathIndex(string[] specificationPaths)
    {
        Dictionary<string, List<string>> pathsById = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (string specificationPath in specificationPaths)
        {
            try
            {
                using StreamReader reader = File.OpenText(specificationPath);
                YamlStream yamlStream = new YamlStream();
                yamlStream.Load(reader);
                if (yamlStream.Documents.Count != 1
                    || yamlStream.Documents[0].RootNode is not YamlMappingNode root
                    || ReadInteger(root, "schema_version") != 3)
                {
                    continue;
                }

                string? id = ReadScalar(root, "id");
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!pathsById.TryGetValue(id, out List<string>? paths))
                {
                    paths = new List<string>();
                    pathsById.Add(id, paths);
                }

                paths.Add(specificationPath);
            }
            catch (YamlException)
            {
            }
            catch (IOException)
            {
            }
        }

        return pathsById;
    }

    private void ValidateDocumentSafely(
        string specificationPath,
        List<SpecificationDocument> documents,
        List<GateIssue> issues,
        ref bool hasConfigurationError)
    {
        try
        {
            ValidateDocument(specificationPath, documents, issues);
        }
        catch (YamlException exception)
        {
            issues.Add(CreateIssue(
                "V3-SPEC-YAML-PARSE",
                specificationPath,
                (int)exception.Start.Line + 1,
                $"YAML 解析失敗：{exception.Message}"));
            hasConfigurationError = true;
        }
        catch (IOException exception)
        {
            issues.Add(CreateIssue(
                "V3-SPEC-FILE-READ",
                specificationPath,
                null,
                $"讀取規格失敗：{exception.Message}"));
            hasConfigurationError = true;
        }
    }

    private void ValidateDocument(
        string specificationPath,
        List<SpecificationDocument> documents,
        List<GateIssue> issues)
    {
        using StreamReader reader = File.OpenText(specificationPath);
        YamlStream yamlStream = new YamlStream();
        yamlStream.Load(reader);
        if (yamlStream.Documents.Count != 1
            || yamlStream.Documents[0].RootNode is not YamlMappingNode root)
        {
            issues.Add(CreateIssue(
                "V3-SPEC-DOCUMENT-ROOT",
                specificationPath,
                null,
                "規格必須包含一個 YAML mapping 文件。"));
            return;
        }

        int? schemaVersion = ReadInteger(root, "schema_version");
        if (schemaVersion != 3)
        {
            issues.Add(CreateIssue(
                "V3-SPEC-SCHEMA-VERSION",
                specificationPath,
                FindLine(root, "schema_version"),
                "schema_version 必須是 3。"));
        }

        string? kindName = ReadScalar(root, "spec_kind");
        if (string.IsNullOrWhiteSpace(kindName)
            || !_schemaRegistry.TryGet(kindName, out SpecificationSchemaDefinition? schema)
            || schema is null)
        {
            issues.Add(CreateIssue(
                "V3-SPEC-KIND",
                specificationPath,
                FindLine(root, "spec_kind"),
                $"不支援的 spec_kind：{kindName ?? "<missing>"}。"));
            return;
        }

        ValidateFields(specificationPath, root, schema, issues);
        string? id = ReadScalar(root, "id");
        if (string.IsNullOrWhiteSpace(id)
            || !Regex.IsMatch(id, "^V3-[A-Z0-9]+(?:-[A-Z0-9]+)+$", RegexOptions.CultureInvariant))
        {
            issues.Add(CreateIssue(
                "V3-SPEC-ID-FORMAT",
                specificationPath,
                FindLine(root, "id"),
                $"規格 id 必須符合 V3-{{KIND}}-{{SCOPE}}-{{NAME}} 格式：{id ?? "<missing>"}。"));
            return;
        }

        List<SpecificationReference> references = ReadReferences(
            specificationPath,
            root,
            issues);
        documents.Add(new SpecificationDocument(
            specificationPath,
            new SpecificationDocumentHeader(schemaVersion ?? 0, schema.SpecificationKind, id),
            references));
    }

    private void ValidateFields(
        string specificationPath,
        YamlMappingNode root,
        SpecificationSchemaDefinition schema,
        List<GateIssue> issues)
    {
        Dictionary<string, SpecificationFieldDefinition> fieldsByName = schema.Fields
            .ToDictionary(field => field.Name, StringComparer.Ordinal);
        HashSet<string> presentFields = new HashSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<YamlNode, YamlNode> child in root.Children)
        {
            if (child.Key is not YamlScalarNode key || string.IsNullOrWhiteSpace(key.Value))
            {
                issues.Add(CreateIssue(
                    "V3-SPEC-FIELD-NAME",
                    specificationPath,
                    (int)child.Key.Start.Line + 1,
                    "頂層欄位名稱必須是非空字串。"));
                continue;
            }

            presentFields.Add(key.Value);
            if (!fieldsByName.TryGetValue(key.Value, out SpecificationFieldDefinition? field))
            {
                issues.Add(CreateIssue(
                    "V3-SPEC-UNKNOWN-FIELD",
                    specificationPath,
                    (int)key.Start.Line + 1,
                    $"{schema.YamlName} 不允許頂層欄位 {key.Value}。"));
                continue;
            }

            if (!HasExpectedNodeType(child.Value, field.NodeType))
            {
                issues.Add(CreateIssue(
                    "V3-SPEC-FIELD-TYPE",
                    specificationPath,
                    (int)child.Value.Start.Line + 1,
                    $"{schema.YamlName} 的 {key.Value} 必須是 {GetNodeTypeName(field.NodeType)}。"));
            }
        }

        foreach (SpecificationFieldDefinition field in schema.Fields.Where(field => field.IsRequired))
        {
            if (!presentFields.Contains(field.Name))
            {
                issues.Add(CreateIssue(
                    "V3-SPEC-REQUIRED-FIELD",
                    specificationPath,
                    null,
                    $"{schema.YamlName} 缺少必要欄位 {field.Name}。"));
            }
        }
    }

    private bool HasExpectedNodeType(YamlNode node, SpecificationFieldNodeType nodeType)
    {
        return nodeType switch
        {
            SpecificationFieldNodeType.Scalar => node is YamlScalarNode,
            SpecificationFieldNodeType.Mapping => node is YamlMappingNode,
            SpecificationFieldNodeType.Sequence => node is YamlSequenceNode,
            _ => false
        };
    }

    private string GetNodeTypeName(SpecificationFieldNodeType nodeType)
    {
        return nodeType switch
        {
            SpecificationFieldNodeType.Scalar => "scalar",
            SpecificationFieldNodeType.Mapping => "mapping",
            SpecificationFieldNodeType.Sequence => "sequence",
            _ => nodeType.ToString()
        };
    }

    private void ValidateUniqueIds(List<SpecificationDocument> documents, List<GateIssue> issues)
    {
        foreach (IGrouping<string, SpecificationDocument> duplicate in documents
            .GroupBy(document => document.Header.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1))
        {
            foreach (SpecificationDocument document in duplicate)
            {
                issues.Add(CreateIssue(
                    "V3-SPEC-DUPLICATE-ID",
                    document.FilePath,
                    null,
                    $"規格 id {duplicate.Key} 重複。"));
            }
        }
    }

    private List<SpecificationReference> ReadReferences(
        string specificationPath,
        YamlMappingNode root,
        List<GateIssue> issues)
    {
        List<SpecificationReference> references = new List<SpecificationReference>();
        ReadScalarReference(specificationPath, root, "frame_ref", SpecificationKind.ProblemFrame, references, issues);
        ReadScalarReference(specificationPath, root, "workflow_ref", SpecificationKind.WorkflowContract, references, issues);
        ReadScalarReference(specificationPath, root, "domain_ref", SpecificationKind.DomainContract, references, issues);
        ReadScalarReference(specificationPath, root, "contract_ref", null, references, issues);
        ReadScalarReference(specificationPath, root, "technical_ref", SpecificationKind.TechnicalContract, references, issues);
        ReadScalarReference(specificationPath, root, "acceptance_ref", SpecificationKind.AcceptanceContract, references, issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "domain_refs",
            SpecificationKind.AggregateContract,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "aggregate_refs",
            SpecificationKind.AggregateContract,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "domain_contract_refs",
            SpecificationKind.DomainContract,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "workflow_refs",
            SpecificationKind.WorkflowContract,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "state_refs",
            SpecificationKind.PresentationState,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "frames",
            SpecificationKind.ProblemFrame,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "aggregates",
            SpecificationKind.AggregateContract,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "domain_contracts",
            SpecificationKind.DomainContract,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "workflows",
            SpecificationKind.WorkflowContract,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "technical_contracts",
            SpecificationKind.TechnicalContract,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "acceptance_contracts",
            SpecificationKind.AcceptanceContract,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "journeys",
            SpecificationKind.JourneyContract,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "presentation_states",
            SpecificationKind.PresentationState,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "presentation_workflows",
            SpecificationKind.PresentationWorkflow,
            references,
            issues);
        ReadSequenceReferences(
            specificationPath,
            root,
            "migrations",
            SpecificationKind.MigrationMap,
            references,
            issues);
        return references;
    }

    private void ReadScalarReference(
        string specificationPath,
        YamlMappingNode root,
        string fieldName,
        SpecificationKind? expectedKind,
        List<SpecificationReference> references,
        List<GateIssue> issues)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode(fieldName), out YamlNode? node))
        {
            return;
        }

        if (node is not YamlScalarNode scalar || string.IsNullOrWhiteSpace(scalar.Value))
        {
            issues.Add(CreateIssue(
                "V3-SPEC-REFERENCE-TYPE",
                specificationPath,
                (int)node.Start.Line + 1,
                $"{fieldName} 必須是非空的規格 ID。"));
            return;
        }

        references.Add(new SpecificationReference(
            fieldName,
            scalar.Value,
            expectedKind,
            (int)scalar.Start.Line + 1));
    }

    private void ReadSequenceReferences(
        string specificationPath,
        YamlMappingNode root,
        string fieldName,
        SpecificationKind expectedKind,
        List<SpecificationReference> references,
        List<GateIssue> issues)
    {
        if (!root.Children.TryGetValue(new YamlScalarNode(fieldName), out YamlNode? node))
        {
            return;
        }

        if (node is not YamlSequenceNode sequence)
        {
            issues.Add(CreateIssue(
                "V3-SPEC-REFERENCE-TYPE",
                specificationPath,
                (int)node.Start.Line + 1,
                $"{fieldName} 必須是規格 ID 清單。"));
            return;
        }

        foreach (YamlNode child in sequence.Children)
        {
            if (child is not YamlScalarNode scalar || string.IsNullOrWhiteSpace(scalar.Value))
            {
                issues.Add(CreateIssue(
                    "V3-SPEC-REFERENCE-TYPE",
                    specificationPath,
                    (int)child.Start.Line + 1,
                    $"{fieldName} 的每個項目都必須是非空的規格 ID。"));
                continue;
            }

            references.Add(new SpecificationReference(
                fieldName,
                scalar.Value,
                expectedKind,
                (int)scalar.Start.Line + 1));
        }
    }

    private void ValidateReferences(List<SpecificationDocument> documents, List<GateIssue> issues)
    {
        Dictionary<string, SpecificationDocument> documentsById = documents
            .GroupBy(document => document.Header.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (SpecificationDocument source in documents)
        {
            foreach (SpecificationReference reference in source.References)
            {
                if (!documentsById.TryGetValue(reference.TargetId, out SpecificationDocument? target))
                {
                    issues.Add(CreateIssue(
                        "V3-SPEC-REFERENCE-MISSING",
                        source.FilePath,
                        reference.Line,
                        $"{reference.FieldName} 引用不存在的規格 {reference.TargetId}。"));
                    continue;
                }

                if (reference.ExpectedKind.HasValue
                    && target.Header.SpecificationKind != reference.ExpectedKind.Value)
                {
                    issues.Add(CreateIssue(
                        "V3-SPEC-REFERENCE-KIND",
                        source.FilePath,
                        reference.Line,
                        $"{reference.FieldName} 必須引用 {reference.ExpectedKind.Value}，但 {reference.TargetId} 是 {target.Header.SpecificationKind}。"));
                }
            }
        }
    }

    private string? ReadScalar(YamlMappingNode root, string fieldName)
    {
        if (root.Children.TryGetValue(new YamlScalarNode(fieldName), out YamlNode? node)
            && node is YamlScalarNode scalar)
        {
            return scalar.Value;
        }

        return null;
    }

    private int? ReadInteger(YamlMappingNode root, string fieldName)
    {
        string? value = ReadScalar(root, fieldName);
        if (int.TryParse(value, out int parsed))
        {
            return parsed;
        }

        return null;
    }

    private int? FindLine(YamlMappingNode root, string fieldName)
    {
        foreach (KeyValuePair<YamlNode, YamlNode> child in root.Children)
        {
            if (child.Key is YamlScalarNode key && key.Value == fieldName)
            {
                return (int)key.Start.Line + 1;
            }
        }

        return null;
    }

    private GateIssue CreateIssue(string ruleId, string file, int? line, string message)
    {
        return new GateIssue(ruleId, "Specifications", file, line, message, "error");
    }
}
