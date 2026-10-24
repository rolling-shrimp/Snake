using YamlDotNet.RepresentationModel;

namespace PatternNetwork.Gates;

public class PatternNetworkValidator
{
    public PatternNetworkValidationResult Validate(string networkPath)
    {
        string fullNetworkPath = Path.GetFullPath(networkPath);
        List<GateIssue> issues = new List<GateIssue>();
        List<PatternNodeDefinition> patternNodes = new List<PatternNodeDefinition>();
        PatternNetworkDefinition definition = new PatternNetworkDefinition(fullNetworkPath, patternNodes);

        try
        {
            string rootDoctrinePath = Path.Combine(fullNetworkPath, "doctrine.yaml");
            YamlMappingNode rootDoctrine = LoadDocument(rootDoctrinePath);
            ValidateSchemaVersion(rootDoctrine, rootDoctrinePath);

            HashSet<string> ruleIds = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> allIds = new HashSet<string>(StringComparer.Ordinal);
            AddRuleIds(GetSequence(rootDoctrine, "rules", rootDoctrinePath), rootDoctrinePath, ruleIds, allIds, issues);

            string patternsPath = Path.Combine(fullNetworkPath, "patterns");
            if (!Directory.Exists(patternsPath))
            {
                throw new DirectoryNotFoundException($"找不到 patterns 目錄：{patternsPath}");
            }

            string[] patternDirectories = Directory.GetDirectories(patternsPath);
            Array.Sort(patternDirectories, StringComparer.Ordinal);
            foreach (string patternDirectory in patternDirectories)
            {
                PatternNodeDefinition patternNode = LoadPatternNode(patternDirectory, rootDoctrinePath, ruleIds, allIds, issues);
                patternNodes.Add(patternNode);
            }

            ValidateDistinctPatternNodes(patternNodes, issues);
            ValidateRuleReferences(patternNodes, ruleIds, issues);
        }
        catch (Exception exception)
        {
            issues.Add(new GateIssue("PN-CONFIG-LOAD", "network", fullNetworkPath, null, exception.Message, "error"));
        }

        patternNodes.Sort((first, second) => string.Compare(first.Name, second.Name, StringComparison.Ordinal));
        return new PatternNetworkValidationResult(definition, SortIssues(issues));
    }

    private PatternNodeDefinition LoadPatternNode(
        string patternDirectory,
        string rootDoctrinePath,
        HashSet<string> ruleIds,
        HashSet<string> allIds,
        List<GateIssue> issues)
    {
        string doctrinePath = Path.Combine(patternDirectory, "doctrine.yaml");
        string prohibitionsPath = Path.Combine(patternDirectory, "prohibitions.yaml");
        string gatePath = Path.Combine(patternDirectory, "gate.yaml");
        string templatePath = Path.Combine(patternDirectory, "template.cs.j2");
        EnsureRequiredFile(doctrinePath);
        EnsureRequiredFile(prohibitionsPath);
        EnsureRequiredFile(gatePath);
        EnsureRequiredFile(templatePath);

        YamlMappingNode doctrine = LoadDocument(doctrinePath);
        YamlMappingNode prohibitions = LoadDocument(prohibitionsPath);
        YamlMappingNode gate = LoadDocument(gatePath);
        ValidateSchemaVersion(doctrine, doctrinePath);
        ValidateSchemaVersion(prohibitions, prohibitionsPath);
        ValidateSchemaVersion(gate, gatePath);

        YamlMappingNode doctrineMetadata = GetMapping(doctrine, "doctrine", doctrinePath);
        string inheritedPath = GetScalar(doctrineMetadata, "inherits", doctrinePath);
        string resolvedInheritedPath = Path.GetFullPath(Path.Combine(patternDirectory, inheritedPath));
        if (!string.Equals(resolvedInheritedPath, Path.GetFullPath(rootDoctrinePath), StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new GateIssue("PN-CONFIG-INHERITANCE", Path.GetFileName(patternDirectory), doctrinePath, null, "節點 doctrine 必須繼承根層 doctrine.yaml。", "error"));
        }

        YamlMappingNode patternNodeMapping = GetMapping(doctrine, "pattern_node", doctrinePath);
        string patternNodeId = GetScalar(patternNodeMapping, "id", doctrinePath);
        string patternNodeName = GetScalar(patternNodeMapping, "name", doctrinePath);
        string directoryName = Path.GetFileName(patternDirectory);
        if (!string.Equals(patternNodeId, directoryName, StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("PN-CONFIG-NODE-ID", patternNodeId, doctrinePath, null, $"pattern_node.id 必須等於目錄名稱 {directoryName}。", "error"));
        }

        ValidatePatternNodeIdentity(prohibitions, gate, patternNodeId, patternNodeName, prohibitionsPath, gatePath, issues);
        ValidateTemplate(templatePath, patternNodeId, issues);
        ValidatePatternNodeName(patternNodeName, doctrinePath, issues);

        AddRuleIds(GetSequence(doctrine, "rules", doctrinePath), doctrinePath, ruleIds, allIds, issues);
        AddRuleIds(GetSequence(prohibitions, "prohibitions", prohibitionsPath), prohibitionsPath, ruleIds, allIds, issues);

        YamlMappingNode project = GetMapping(doctrine, "project", doctrinePath);
        PatternNodeDefinition definition = new PatternNodeDefinition
        {
            Id = patternNodeId,
            Name = patternNodeName,
            DirectoryPath = patternDirectory,
            TargetFramework = GetScalar(project, "target_framework", doctrinePath),
            UseWindowsForms = bool.Parse(GetScalar(project, "use_windows_forms", doctrinePath)),
            AllowedProjectReferences = GetScalarList(project, "allowed_project_references", doctrinePath),
            Checks = LoadChecks(gate, gatePath, allIds, issues)
        };
        ValidateGateConfiguration(definition, gatePath, issues);

        return definition;
    }

    private void ValidateGateConfiguration(PatternNodeDefinition patternNode, string gatePath, List<GateIssue> issues)
    {
        GateCheckDefinition? identityCheck = patternNode.Checks.FirstOrDefault(check => string.Equals(check.Checker, "project_identity", StringComparison.Ordinal));
        if (identityCheck == null
            || !identityCheck.ScalarParameters.TryGetValue("target_framework", out string? targetFramework)
            || !identityCheck.ScalarParameters.TryGetValue("use_windows_forms", out string? useWindowsFormsText)
            || !bool.TryParse(useWindowsFormsText, out bool useWindowsForms)
            || !string.Equals(targetFramework, patternNode.TargetFramework, StringComparison.Ordinal)
            || useWindowsForms != patternNode.UseWindowsForms)
        {
            issues.Add(new GateIssue("PN-CONFIG-GATE-DRIFT", patternNode.Id, gatePath, null, "project_identity 參數必須與節點 doctrine 的 project 設定一致。", "error"));
        }

        GateCheckDefinition? referenceCheck = patternNode.Checks.FirstOrDefault(check => string.Equals(check.Checker, "project_references", StringComparison.Ordinal));
        if (referenceCheck == null
            || !referenceCheck.ListParameters.TryGetValue("required_pattern_nodes", out List<string>? requiredPatternNodes)
            || !requiredPatternNodes.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(patternNode.AllowedProjectReferences.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal))
        {
            issues.Add(new GateIssue("PN-CONFIG-GATE-DRIFT", patternNode.Id, gatePath, null, "project_references 的 required_pattern_nodes 必須與 doctrine 的 allowed_project_references 一致。", "error"));
        }
    }

    private void ValidatePatternNodeIdentity(
        YamlMappingNode prohibitions,
        YamlMappingNode gate,
        string expectedId,
        string expectedName,
        string prohibitionsPath,
        string gatePath,
        List<GateIssue> issues)
    {
        YamlMappingNode prohibitionNode = GetMapping(prohibitions, "pattern_node", prohibitionsPath);
        string prohibitionId = GetScalar(prohibitionNode, "id", prohibitionsPath);
        string prohibitionName = GetScalar(prohibitionNode, "name", prohibitionsPath);
        YamlMappingNode gateMetadata = GetMapping(gate, "gate", gatePath);
        string gateNodeId = GetScalar(gateMetadata, "pattern_node_id", gatePath);
        string formula = GetScalar(gateMetadata, "project_name_formula", gatePath);

        if (!string.Equals(prohibitionId, expectedId, StringComparison.Ordinal) || !string.Equals(prohibitionName, expectedName, StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("PN-CONFIG-NODE-MISMATCH", expectedId, prohibitionsPath, null, "prohibitions.yaml 的 pattern node 與 doctrine.yaml 不一致。", "error"));
        }

        if (!string.Equals(gateNodeId, expectedId, StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("PN-CONFIG-NODE-MISMATCH", expectedId, gatePath, null, "gate.yaml 的 pattern_node_id 與 doctrine.yaml 不一致。", "error"));
        }

        if (!string.Equals(formula, "{solution_name}.{pattern_node_name}", StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("PN-CONFIG-PROJECT-FORMULA", expectedId, gatePath, null, "project_name_formula 必須使用共用命名公式。", "error"));
        }
    }

    private List<GateCheckDefinition> LoadChecks(YamlMappingNode gate, string gatePath, HashSet<string> allIds, List<GateIssue> issues)
    {
        HashSet<string> knownCheckers = new HashSet<string>(StringComparer.Ordinal)
        {
            "project_identity",
            "project_references",
            "csharp_global_rules",
            "forbidden_namespaces",
            "entity_boundaries",
            "use_case_contracts",
            "use_case_entity_exposure",
            "winforms_boundaries",
            "top_level_program",
            "manual_review"
        };
        List<GateCheckDefinition> checks = new List<GateCheckDefinition>();
        foreach (YamlNode checkNode in GetSequence(gate, "checks", gatePath))
        {
            YamlMappingNode checkMapping = RequireMapping(checkNode, gatePath, "checks");
            GateCheckDefinition check = new GateCheckDefinition
            {
                Id = GetScalar(checkMapping, "id", gatePath),
                Checker = GetScalar(checkMapping, "checker", gatePath),
                Severity = GetScalar(checkMapping, "severity", gatePath),
                RuleReferences = GetScalarList(checkMapping, "rule_refs", gatePath)
            };

            AddUniqueId(check.Id, gatePath, allIds, issues);
            if (!knownCheckers.Contains(check.Checker))
            {
                issues.Add(new GateIssue("PN-CONFIG-CHECKER", "network", gatePath, null, $"未知 checker：{check.Checker}", "error"));
            }

            if (TryGetNode(checkMapping, "parameters", out YamlNode? parametersNode))
            {
                YamlMappingNode parameters = RequireMapping(parametersNode!, gatePath, "parameters");
                foreach (KeyValuePair<YamlNode, YamlNode> parameter in parameters.Children)
                {
                    string key = RequireScalar(parameter.Key, gatePath, "parameter key");
                    if (parameter.Value is YamlSequenceNode sequence)
                    {
                        check.ListParameters[key] = ReadScalarList(sequence, gatePath, key);
                    }
                    else
                    {
                        check.ScalarParameters[key] = RequireScalar(parameter.Value, gatePath, key);
                    }
                }
            }

            checks.Add(check);
        }

        return checks;
    }

    private void ValidateRuleReferences(List<PatternNodeDefinition> patternNodes, HashSet<string> ruleIds, List<GateIssue> issues)
    {
        foreach (PatternNodeDefinition patternNode in patternNodes)
        {
            foreach (GateCheckDefinition check in patternNode.Checks)
            {
                foreach (string ruleReference in check.RuleReferences)
                {
                    if (!ruleIds.Contains(ruleReference))
                    {
                        issues.Add(new GateIssue("PN-CONFIG-RULE-REF", patternNode.Id, Path.Combine(patternNode.DirectoryPath, "gate.yaml"), null, $"{check.Id} 引用了不存在的規則 {ruleReference}。", "error"));
                    }
                }
            }
        }
    }

    private void ValidateDistinctPatternNodes(List<PatternNodeDefinition> patternNodes, List<GateIssue> issues)
    {
        foreach (IGrouping<string, PatternNodeDefinition> group in patternNodes.GroupBy(node => node.Id, StringComparer.Ordinal))
        {
            if (group.Count() > 1)
            {
                issues.Add(new GateIssue("PN-CONFIG-DUPLICATE-NODE", group.Key, group.First().DirectoryPath, null, $"pattern node id 重複：{group.Key}", "error"));
            }
        }

        foreach (IGrouping<string, PatternNodeDefinition> group in patternNodes.GroupBy(node => node.Name, StringComparer.Ordinal))
        {
            if (group.Count() > 1)
            {
                issues.Add(new GateIssue("PN-CONFIG-DUPLICATE-NODE", group.Key, group.First().DirectoryPath, null, $"pattern node name 重複：{group.Key}", "error"));
            }
        }
    }

    private void ValidatePatternNodeName(string patternNodeName, string doctrinePath, List<GateIssue> issues)
    {
        try
        {
            ProjectNamePolicy policy = new ProjectNamePolicy();
            policy.GetProjectName("Sample.sln", patternNodeName);
        }
        catch (ArgumentException exception)
        {
            issues.Add(new GateIssue("PN-CONFIG-NODE-NAME", patternNodeName, doctrinePath, null, exception.Message, "error"));
        }
    }

    private void ValidateTemplate(string templatePath, string patternNodeId, List<GateIssue> issues)
    {
        string templateText = File.ReadAllText(templatePath);
        if (!templateText.Contains("{{ namespace_name }}", StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("PN-CONFIG-TEMPLATE", patternNodeId, templatePath, null, "template.cs.j2 必須使用 namespace_name 變數。", "error"));
        }

        if (!templateText.Contains("public class", StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("PN-CONFIG-TEMPLATE", patternNodeId, templatePath, null, "template.cs.j2 必須產生一個 public class。", "error"));
        }
    }

    private void AddRuleIds(YamlSequenceNode rules, string filePath, HashSet<string> ruleIds, HashSet<string> allIds, List<GateIssue> issues)
    {
        foreach (YamlNode ruleNode in rules)
        {
            YamlMappingNode rule = RequireMapping(ruleNode, filePath, "rule");
            string id = GetScalar(rule, "id", filePath);
            ruleIds.Add(id);
            AddUniqueId(id, filePath, allIds, issues);
        }
    }

    private void AddUniqueId(string id, string filePath, HashSet<string> allIds, List<GateIssue> issues)
    {
        if (!allIds.Add(id))
        {
            issues.Add(new GateIssue("PN-CONFIG-DUPLICATE-ID", "network", filePath, null, $"規則或檢查 ID 重複：{id}", "error"));
        }
    }

    private YamlMappingNode LoadDocument(string filePath)
    {
        try
        {
            using StreamReader reader = File.OpenText(filePath);
            YamlStream stream = new YamlStream();
            stream.Load(reader);
            if (stream.Documents.Count != 1)
            {
                throw new InvalidDataException($"YAML 必須剛好包含一份文件：{filePath}");
            }

            return RequireMapping(stream.Documents[0].RootNode, filePath, "document root");
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"讀取 YAML 失敗：{filePath}；{exception.Message}", exception);
        }
    }

    private void ValidateSchemaVersion(YamlMappingNode document, string filePath)
    {
        string version = GetScalar(document, "schema_version", filePath);
        if (!string.Equals(version, "1", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"不支援的 schema_version：{version}；檔案：{filePath}");
        }
    }

    private YamlMappingNode GetMapping(YamlMappingNode parent, string key, string filePath)
    {
        if (!TryGetNode(parent, key, out YamlNode? node))
        {
            throw new InvalidDataException($"缺少 {key}：{filePath}");
        }

        return RequireMapping(node!, filePath, key);
    }

    private YamlSequenceNode GetSequence(YamlMappingNode parent, string key, string filePath)
    {
        if (!TryGetNode(parent, key, out YamlNode? node) || node is not YamlSequenceNode sequence)
        {
            throw new InvalidDataException($"{key} 必須是陣列：{filePath}");
        }

        return sequence;
    }

    private string GetScalar(YamlMappingNode parent, string key, string filePath)
    {
        if (!TryGetNode(parent, key, out YamlNode? node))
        {
            throw new InvalidDataException($"缺少 {key}：{filePath}");
        }

        return RequireScalar(node!, filePath, key);
    }

    private List<string> GetScalarList(YamlMappingNode parent, string key, string filePath)
    {
        return ReadScalarList(GetSequence(parent, key, filePath), filePath, key);
    }

    private List<string> ReadScalarList(YamlSequenceNode sequence, string filePath, string key)
    {
        List<string> values = new List<string>();
        foreach (YamlNode item in sequence)
        {
            values.Add(RequireScalar(item, filePath, key));
        }

        return values;
    }

    private bool TryGetNode(YamlMappingNode parent, string key, out YamlNode? node)
    {
        return parent.Children.TryGetValue(new YamlScalarNode(key), out node);
    }

    private YamlMappingNode RequireMapping(YamlNode node, string filePath, string key)
    {
        if (node is not YamlMappingNode mapping)
        {
            throw new InvalidDataException($"{key} 必須是物件：{filePath}");
        }

        return mapping;
    }

    private string RequireScalar(YamlNode node, string filePath, string key)
    {
        if (node is not YamlScalarNode scalar || scalar.Value == null)
        {
            throw new InvalidDataException($"{key} 必須是字串：{filePath}");
        }

        return scalar.Value;
    }

    private void EnsureRequiredFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"缺少 Pattern Network 必要檔案：{filePath}", filePath);
        }
    }

    private List<GateIssue> SortIssues(List<GateIssue> issues)
    {
        return issues
            .OrderBy(issue => issue.Node, StringComparer.Ordinal)
            .ThenBy(issue => issue.File, StringComparer.Ordinal)
            .ThenBy(issue => issue.Line)
            .ThenBy(issue => issue.RuleId, StringComparer.Ordinal)
            .ToList();
    }
}
