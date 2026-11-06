using YamlDotNet.RepresentationModel;

namespace PatternNetwork.Gates;

public class MigrationMapValidator
{
    public MigrationMapValidationResult Validate(string domainsPath, string networkPath)
    {
        List<GateIssue> issues = new List<GateIssue>();
        bool hasConfigurationError = false;

        ValidateRoot("Domains", domainsPath, issues, ref hasConfigurationError);
        ValidateRoot("Pattern Network", networkPath, issues, ref hasConfigurationError);

        return new MigrationMapValidationResult(issues, hasConfigurationError);
    }

    private void ValidateRoot(
        string rootName,
        string rootPath,
        List<GateIssue> issues,
        ref bool hasConfigurationError)
    {
        string fullRootPath;
        try
        {
            fullRootPath = Path.GetFullPath(rootPath);
        }
        catch (Exception exception)
        {
            issues.Add(CreateIssue(
                "V3-MIGRATION-CONFIG-ROOT",
                rootName,
                rootPath,
                null,
                $"遷移來源根目錄無效：{exception.Message}"));
            hasConfigurationError = true;
            return;
        }

        if (!Directory.Exists(fullRootPath))
        {
            issues.Add(CreateIssue(
                "V3-MIGRATION-CONFIG-ROOT",
                rootName,
                fullRootPath,
                null,
                "找不到遷移來源根目錄。"));
            hasConfigurationError = true;
            return;
        }

        Dictionary<string, YamlMappingNode> documents = LoadDocuments(
            rootName,
            fullRootPath,
            issues,
            ref hasConfigurationError);
        string migrationMapPath = "migration-map.yaml";
        if (!documents.TryGetValue(migrationMapPath, out YamlMappingNode? migrationMap))
        {
            issues.Add(CreateIssue(
                "V3-MIGRATION-CONFIG-MAP",
                rootName,
                Path.Combine(fullRootPath, migrationMapPath),
                null,
                "找不到 migration-map.yaml。"));
            hasConfigurationError = true;
            return;
        }

        HashSet<string> targetIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, HashSet<string>> legacyIdsBySource = new Dictionary<string, HashSet<string>>(
            StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, YamlMappingNode> document in documents)
        {
            if (IsSchemaV3(document.Value))
            {
                CollectCurrentIds(document.Value, targetIds);
            }
            else if (!string.Equals(document.Key, migrationMapPath, StringComparison.OrdinalIgnoreCase))
            {
                HashSet<string> legacyIds = new HashSet<string>(StringComparer.Ordinal);
                CollectLegacyIds(document.Value, legacyIds);
                legacyIdsBySource.Add(document.Key, legacyIds);
            }
        }

        ValidateMap(
            rootName,
            fullRootPath,
            migrationMap,
            targetIds,
            legacyIdsBySource,
            issues);
    }

    private Dictionary<string, YamlMappingNode> LoadDocuments(
        string rootName,
        string rootPath,
        List<GateIssue> issues,
        ref bool hasConfigurationError)
    {
        Dictionary<string, YamlMappingNode> documents = new Dictionary<string, YamlMappingNode>(
            StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> yamlFiles;
        try
        {
            yamlFiles = Directory.EnumerateFiles(rootPath, "*.yaml", SearchOption.AllDirectories).ToList();
        }
        catch (Exception exception)
        {
            issues.Add(CreateIssue(
                "V3-MIGRATION-CONFIG-ROOT",
                rootName,
                rootPath,
                null,
                $"列舉 YAML 失敗：{exception.Message}"));
            hasConfigurationError = true;
            return documents;
        }

        foreach (string yamlFile in yamlFiles)
        {
            try
            {
                using StreamReader reader = File.OpenText(yamlFile);
                YamlStream yaml = new YamlStream();
                yaml.Load(reader);
                if (yaml.Documents.Count != 1 || yaml.Documents[0].RootNode is not YamlMappingNode root)
                {
                    issues.Add(CreateIssue(
                        "V3-MIGRATION-CONFIG-YAML",
                        rootName,
                        yamlFile,
                        null,
                        "YAML 必須包含一份 mapping document。"));
                    hasConfigurationError = true;
                    continue;
                }

                string relativePath = NormalizeRelativePath(Path.GetRelativePath(rootPath, yamlFile));
                documents.Add(relativePath, root);
            }
            catch (Exception exception)
            {
                issues.Add(CreateIssue(
                    "V3-MIGRATION-CONFIG-YAML",
                    rootName,
                    yamlFile,
                    null,
                    $"讀取 YAML 失敗：{exception.Message}"));
                hasConfigurationError = true;
            }
        }

        return documents;
    }

    private void ValidateMap(
        string rootName,
        string rootPath,
        YamlMappingNode migrationMap,
        HashSet<string> targetIds,
        Dictionary<string, HashSet<string>> legacyIdsBySource,
        List<GateIssue> issues)
    {
        string mapFile = Path.Combine(rootPath, "migration-map.yaml");
        if (GetScalar(migrationMap, "schema_version") != "3"
            || GetScalar(migrationMap, "spec_kind") != "migration_map"
            || string.IsNullOrWhiteSpace(GetScalar(migrationMap, "id")))
        {
            issues.Add(CreateIssue(
                "V3-MIGRATION-MAP-FORM",
                rootName,
                mapFile,
                null,
                "migration map 必須宣告 schema_version: 3、spec_kind: migration_map 與 id。"));
        }

        if (GetNode(migrationMap, "entries") is not YamlSequenceNode entries)
        {
            issues.Add(CreateIssue(
                "V3-MIGRATION-MAP-FORM",
                rootName,
                mapFile,
                GetLine(migrationMap),
                "entries 必須是 sequence。"));
            return;
        }

        HashSet<string> mappedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> mappedLegacyIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (YamlNode entryNode in entries.Children)
        {
            if (entryNode is not YamlMappingNode entry)
            {
                issues.Add(CreateIssue(
                    "V3-MIGRATION-MAP-FORM",
                    rootName,
                    mapFile,
                    GetLine(entryNode),
                    "每個 migration entry 必須是 mapping。"));
                continue;
            }

            List<string> sources = ReadSources(rootName, rootPath, mapFile, entry, issues);
            foreach (string source in sources)
            {
                if (!mappedSources.Add(source))
                {
                    issues.Add(CreateIssue(
                        "V3-MIGRATION-SOURCE-DUPLICATE",
                        rootName,
                        mapFile,
                        GetLine(entry),
                        $"來源檔案被多個 entry 處置：{source}"));
                }
            }

            List<string> legacyIds = ReadScalarSequence(
                rootName,
                mapFile,
                entry,
                "legacy_ids",
                true,
                issues,
                "V3-MIGRATION-LEGACY-ID-FORM");
            foreach (string legacyId in legacyIds)
            {
                if (!mappedLegacyIds.Add(legacyId))
                {
                    issues.Add(CreateIssue(
                        "V3-MIGRATION-LEGACY-ID-DUPLICATE",
                        rootName,
                        mapFile,
                        GetLine(entry),
                        $"legacy ID 被多個 entry 處置：{legacyId}"));
                }
            }

            ValidateDisposition(rootName, mapFile, entry, targetIds, issues);
        }

        foreach (KeyValuePair<string, HashSet<string>> legacyDocument in legacyIdsBySource)
        {
            if (!mappedSources.Contains(legacyDocument.Key))
            {
                issues.Add(CreateIssue(
                    "V3-MIGRATION-LEGACY-SOURCE-MISSING",
                    rootName,
                    Path.Combine(rootPath, legacyDocument.Key),
                    null,
                    $"legacy 來源沒有 migration disposition：{legacyDocument.Key}"));
            }

            foreach (string legacyId in legacyDocument.Value)
            {
                if (!mappedLegacyIds.Contains(legacyId))
                {
                    issues.Add(CreateIssue(
                        "V3-MIGRATION-LEGACY-ID-MISSING",
                        rootName,
                        Path.Combine(rootPath, legacyDocument.Key),
                        null,
                        $"legacy ID 沒有 migration disposition：{legacyId}"));
                }
            }
        }
    }

    private List<string> ReadSources(
        string rootName,
        string rootPath,
        string mapFile,
        YamlMappingNode entry,
        List<GateIssue> issues)
    {
        YamlNode? sourcePathNode = GetNode(entry, "source_path");
        YamlNode? sourcePathsNode = GetNode(entry, "source_paths");
        if ((sourcePathNode == null) == (sourcePathsNode == null))
        {
            issues.Add(CreateIssue(
                "V3-MIGRATION-SOURCE-FORM",
                rootName,
                mapFile,
                GetLine(entry),
                "entry 必須且只能宣告 source_path 或 source_paths。"));
            return new List<string>();
        }

        List<string> sourceValues = new List<string>();
        if (sourcePathNode is YamlScalarNode sourceScalar && !string.IsNullOrWhiteSpace(sourceScalar.Value))
        {
            sourceValues.Add(sourceScalar.Value);
        }
        else if (sourcePathsNode is YamlSequenceNode sourceSequence)
        {
            foreach (YamlNode item in sourceSequence.Children)
            {
                if (item is YamlScalarNode itemScalar && !string.IsNullOrWhiteSpace(itemScalar.Value))
                {
                    sourceValues.Add(itemScalar.Value);
                }
                else
                {
                    issues.Add(CreateIssue(
                        "V3-MIGRATION-SOURCE-FORM",
                        rootName,
                        mapFile,
                        GetLine(item),
                        "source_paths 只能包含非空字串。"));
                }
            }
        }
        else
        {
            issues.Add(CreateIssue(
                "V3-MIGRATION-SOURCE-FORM",
                rootName,
                mapFile,
                GetLine(entry),
                "source_path 必須是字串，source_paths 必須是 sequence。"));
        }

        if (sourceValues.Count == 0)
        {
            issues.Add(CreateIssue(
                "V3-MIGRATION-SOURCE-FORM",
                rootName,
                mapFile,
                GetLine(entry),
                "entry 至少需要一個來源檔案。"));
        }

        List<string> normalizedSources = new List<string>();
        foreach (string sourceValue in sourceValues)
        {
            if (TryNormalizeSource(rootPath, sourceValue, out string? normalizedSource))
            {
                normalizedSources.Add(normalizedSource!);
            }
            else
            {
                issues.Add(CreateIssue(
                    "V3-MIGRATION-SOURCE-PATH",
                    rootName,
                    mapFile,
                    GetLine(entry),
                    $"source path 必須是根目錄內的相對 YAML 路徑：{sourceValue}"));
            }
        }

        return normalizedSources;
    }

    private void ValidateDisposition(
        string rootName,
        string mapFile,
        YamlMappingNode entry,
        HashSet<string> targetIds,
        List<GateIssue> issues)
    {
        string? disposition = GetScalar(entry, "disposition");
        string? targetId = GetScalar(entry, "target_id");
        List<string> targetIdList = ReadOptionalScalarSequence(
            rootName,
            mapFile,
            entry,
            "target_ids",
            issues,
            "V3-MIGRATION-TARGET-FORM");
        List<string> targets = new List<string>();

        if (disposition == "replaced_by" || disposition == "merged_into")
        {
            if (string.IsNullOrWhiteSpace(targetId) || targetIdList.Count > 0 || GetNode(entry, "target_ids") != null)
            {
                issues.Add(CreateIssue(
                    "V3-MIGRATION-TARGET-FORM",
                    rootName,
                    mapFile,
                    GetLine(entry),
                    $"{disposition} 必須只宣告一個 target_id。"));
            }
            else
            {
                targets.Add(targetId);
            }
        }
        else if (disposition == "split_into")
        {
            if (!string.IsNullOrWhiteSpace(targetId) || targetIdList.Count < 2)
            {
                issues.Add(CreateIssue(
                    "V3-MIGRATION-TARGET-FORM",
                    rootName,
                    mapFile,
                    GetLine(entry),
                    "split_into 必須只宣告至少兩個 target_ids。"));
            }
            else
            {
                targets.AddRange(targetIdList);
            }
        }
        else if (disposition == "retired")
        {
            if (!string.IsNullOrWhiteSpace(targetId) || GetNode(entry, "target_ids") != null)
            {
                issues.Add(CreateIssue(
                    "V3-MIGRATION-TARGET-FORM",
                    rootName,
                    mapFile,
                    GetLine(entry),
                    "retired 不得宣告 target_id 或 target_ids。"));
            }

            if (string.IsNullOrWhiteSpace(GetScalar(entry, "reason")))
            {
                issues.Add(CreateIssue(
                    "V3-MIGRATION-RETIRED-REASON",
                    rootName,
                    mapFile,
                    GetLine(entry),
                    "retired entry 必須說明 reason。"));
            }
        }
        else
        {
            issues.Add(CreateIssue(
                "V3-MIGRATION-DISPOSITION",
                rootName,
                mapFile,
                GetLine(entry),
                $"不支援的 disposition：{disposition ?? "<missing>"}"));
        }

        foreach (string target in targets.Distinct(StringComparer.Ordinal))
        {
            if (!targetIds.Contains(target))
            {
                issues.Add(CreateIssue(
                    "V3-MIGRATION-TARGET-MISSING",
                    rootName,
                    mapFile,
                    GetLine(entry),
                    $"找不到 migration target ID：{target}"));
            }
        }
    }

    private List<string> ReadScalarSequence(
        string rootName,
        string mapFile,
        YamlMappingNode mapping,
        string fieldName,
        bool required,
        List<GateIssue> issues,
        string ruleId)
    {
        YamlNode? node = GetNode(mapping, fieldName);
        if (node == null)
        {
            if (required)
            {
                issues.Add(CreateIssue(
                    ruleId,
                    rootName,
                    mapFile,
                    GetLine(mapping),
                    $"{fieldName} 必須是 sequence。"));
            }

            return new List<string>();
        }

        if (node is not YamlSequenceNode sequence)
        {
            issues.Add(CreateIssue(
                ruleId,
                rootName,
                mapFile,
                GetLine(node),
                $"{fieldName} 必須是 sequence。"));
            return new List<string>();
        }

        List<string> values = new List<string>();
        foreach (YamlNode item in sequence.Children)
        {
            if (item is YamlScalarNode scalar && !string.IsNullOrWhiteSpace(scalar.Value))
            {
                values.Add(scalar.Value);
            }
            else
            {
                issues.Add(CreateIssue(
                    ruleId,
                    rootName,
                    mapFile,
                    GetLine(item),
                    $"{fieldName} 只能包含非空字串。"));
            }
        }

        return values;
    }

    private List<string> ReadOptionalScalarSequence(
        string rootName,
        string mapFile,
        YamlMappingNode mapping,
        string fieldName,
        List<GateIssue> issues,
        string ruleId)
    {
        return ReadScalarSequence(rootName, mapFile, mapping, fieldName, false, issues, ruleId);
    }

    private bool TryNormalizeSource(string rootPath, string sourcePath, out string? normalizedSource)
    {
        normalizedSource = null;
        if (Path.IsPathRooted(sourcePath)
            || !string.Equals(Path.GetExtension(sourcePath), ".yaml", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string fullSourcePath;
        try
        {
            fullSourcePath = Path.GetFullPath(Path.Combine(rootPath, sourcePath));
        }
        catch
        {
            return false;
        }

        string relativePath = Path.GetRelativePath(rootPath, fullSourcePath);
        if (relativePath == ".."
            || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath))
        {
            return false;
        }

        normalizedSource = NormalizeRelativePath(relativePath);
        return true;
    }

    private void CollectCurrentIds(YamlNode node, HashSet<string> ids)
    {
        if (node is YamlMappingNode mapping)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> child in mapping.Children)
            {
                if (child.Key is YamlScalarNode key
                    && key.Value == "id"
                    && child.Value is YamlScalarNode value
                    && !string.IsNullOrWhiteSpace(value.Value))
                {
                    ids.Add(value.Value);
                }

                CollectCurrentIds(child.Value, ids);
            }
        }
        else if (node is YamlSequenceNode sequence)
        {
            foreach (YamlNode child in sequence.Children)
            {
                CollectCurrentIds(child, ids);
            }
        }
    }

    private void CollectLegacyIds(YamlMappingNode root, HashSet<string> ids)
    {
        YamlNode? useCase = GetNode(root, "use_case");
        if (useCase is YamlScalarNode useCaseScalar && !string.IsNullOrWhiteSpace(useCaseScalar.Value))
        {
            ids.Add(useCaseScalar.Value);
        }
        else if (useCase is YamlMappingNode useCaseMapping)
        {
            string? useCaseName = GetScalar(useCaseMapping, "name");
            if (!string.IsNullOrWhiteSpace(useCaseName))
            {
                ids.Add(useCaseName);
            }
        }

        CollectLegacyDefinitionIds(root, ids);
    }

    private void CollectLegacyDefinitionIds(YamlNode node, HashSet<string> ids)
    {
        HashSet<string> definitionFields = new HashSet<string>(StringComparer.Ordinal)
        {
            "id",
            "rule_id",
            "check_id",
            "mapping_id",
            "concern_id",
            "relation_id"
        };
        if (node is YamlMappingNode mapping)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> child in mapping.Children)
            {
                if (child.Key is YamlScalarNode key
                    && key.Value != null
                    && definitionFields.Contains(key.Value)
                    && child.Value is YamlScalarNode value
                    && !string.IsNullOrWhiteSpace(value.Value))
                {
                    ids.Add(value.Value);
                }

                CollectLegacyDefinitionIds(child.Value, ids);
            }
        }
        else if (node is YamlSequenceNode sequence)
        {
            foreach (YamlNode child in sequence.Children)
            {
                CollectLegacyDefinitionIds(child, ids);
            }
        }
    }

    private bool IsSchemaV3(YamlMappingNode mapping)
    {
        return GetScalar(mapping, "schema_version") == "3";
    }

    private YamlNode? GetNode(YamlMappingNode mapping, string fieldName)
    {
        foreach (KeyValuePair<YamlNode, YamlNode> child in mapping.Children)
        {
            if (child.Key is YamlScalarNode key && key.Value == fieldName)
            {
                return child.Value;
            }
        }

        return null;
    }

    private string? GetScalar(YamlMappingNode mapping, string fieldName)
    {
        return GetNode(mapping, fieldName) is YamlScalarNode scalar ? scalar.Value : null;
    }

    private int? GetLine(YamlNode node)
    {
        return (int)node.Start.Line + 1;
    }

    private string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private GateIssue CreateIssue(
        string ruleId,
        string node,
        string file,
        int? line,
        string message)
    {
        return new GateIssue(ruleId, node, file, line, message, "error");
    }
}
