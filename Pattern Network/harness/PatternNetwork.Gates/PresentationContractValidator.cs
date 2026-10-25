using System.Buffers.Binary;
using YamlDotNet.RepresentationModel;

namespace PatternNetwork.Gates;

public class PresentationContractValidator
{
    public PresentationContractValidationResult Validate(string domainsPath, string projectRoot)
    {
        List<GateIssue> issues = new List<GateIssue>();
        string fullDomainsPath = Path.GetFullPath(domainsPath);
        string fullProjectRoot = Path.GetFullPath(projectRoot);
        if (!Directory.Exists(fullDomainsPath))
        {
            issues.Add(CreateIssue("PRES-CONFIG-DOMAINS", domainsPath, $"找不到 Domains 資料夾：{domainsPath}"));
            return new PresentationContractValidationResult(issues, true);
        }

        if (!Directory.Exists(fullProjectRoot))
        {
            issues.Add(CreateIssue("PRES-CONFIG-PROJECT", projectRoot, $"找不到專案根目錄：{projectRoot}"));
            return new PresentationContractValidationResult(issues, true);
        }

        string presentationPath = Path.Combine(fullDomainsPath, "Game", "presentation");
        string catalogPath = Path.Combine(presentationPath, "presentation_catalog.yaml");
        if (!File.Exists(catalogPath))
        {
            issues.Add(CreateIssue("PRES-CONFIG-CATALOG", catalogPath, "找不到 presentation_catalog.yaml。"));
            return new PresentationContractValidationResult(issues, true);
        }

        try
        {
            YamlMappingNode catalog = LoadYaml(catalogPath);
            ValidateCatalog(catalog, catalogPath, presentationPath, fullProjectRoot, issues);
            ValidateNoReverseRelations(fullDomainsPath, presentationPath, issues);
        }
        catch (Exception exception)
        {
            issues.Add(CreateIssue("PRES-CONFIG-YAML", catalogPath, $"讀取 presentation YAML 失敗：{exception.Message}"));
            return new PresentationContractValidationResult(issues, true);
        }

        return new PresentationContractValidationResult(issues, false);
    }

    private void ValidateCatalog(
        YamlMappingNode catalog,
        string catalogPath,
        string presentationPath,
        string projectRoot,
        List<GateIssue> issues)
    {
        ValidateSchemaVersion(catalog, catalogPath, issues);
        YamlMappingNode presentation = GetMapping(catalog, "presentation", catalogPath);
        ValidateValue(presentation, "bounded_context", "snake-game", catalogPath, issues);
        ValidateValue(presentation, "aggregate", "game", catalogPath, issues);
        ValidateValue(presentation, "owner", "WinForms", catalogPath, issues);
        ValidateValue(presentation, "relation_ownership", "reference_only", catalogPath, issues);

        YamlSequenceNode screenEntries = GetSequence(catalog, "screens", catalogPath);
        Dictionary<string, string> expectedScreens = GetExpectedScreens();
        Dictionary<string, string> contractPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        HashSet<string> catalogScreenIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> relationIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (YamlNode entryNode in screenEntries.Children)
        {
            YamlMappingNode entry = RequireMapping(entryNode, catalogPath, "screens 項目");
            string screenId = GetScalar(entry, "screen_id", catalogPath);
            string relativePath = GetScalar(entry, "path", catalogPath);
            if (!catalogScreenIds.Add(screenId))
            {
                issues.Add(CreateIssue("PRES-CATALOG-DUPLICATE", catalogPath, $"畫面 ID 重複：{screenId}"));
                continue;
            }

            if (expectedScreens.TryGetValue(screenId, out string? expectedPath) && relativePath != expectedPath)
            {
                issues.Add(CreateIssue(
                    "PRES-CATALOG-PATH",
                    catalogPath,
                    $"{screenId} 必須使用路徑 {expectedPath}，實際為 {relativePath}。"));
            }

            string contractPath = ResolveProjectPath(presentationPath, relativePath, projectRoot, catalogPath);
            if (!File.Exists(contractPath))
            {
                issues.Add(CreateIssue("PRES-CONTRACT-NOT-FOUND", contractPath, $"找不到畫面契約：{relativePath}"));
                continue;
            }

            YamlMappingNode contract = LoadYaml(contractPath);
            contractPaths[screenId] = contractPath;
            ValidateContract(screenId, contract, contractPath, projectRoot, relationIds, issues);
        }

        ValidateExactScreens(expectedScreens, catalogScreenIds, catalogPath, issues);
        ValidateTransitions(contractPaths, issues);
        ValidateAssetPairs(contractPaths, projectRoot, issues);
    }

    private void ValidateContract(
        string catalogScreenId,
        YamlMappingNode contract,
        string contractPath,
        string projectRoot,
        HashSet<string> relationIds,
        List<GateIssue> issues)
    {
        ValidateSchemaVersion(contract, contractPath, issues);
        YamlMappingNode screen = GetMapping(contract, "screen", contractPath);
        ValidateValue(screen, "screen_id", catalogScreenId, contractPath, issues);
        ValidateValue(screen, "bounded_context", "snake-game", contractPath, issues);
        ValidateValue(screen, "aggregate", "game", contractPath, issues);

        YamlMappingNode grain = GetMapping(contract, "grain", contractPath);
        ValidateValue(grain, "input", "use_case_results_and_read_models", contractPath, issues);
        ValidateValue(grain, "output", "rendered_screen", contractPath, issues);
        ValidateValue(grain, "contract_owner", "WinForms", contractPath, issues);

        YamlSequenceNode relations = GetSequence(contract, "presentation_relations", contractPath);
        if (relations.Children.Count == 0)
        {
            issues.Add(CreateIssue("PRES-RELATION-MISSING", contractPath, "每個畫面至少要有一條 presentation relation。"));
        }

        foreach (YamlNode relationNode in relations.Children)
        {
            YamlMappingNode relation = RequireMapping(relationNode, contractPath, "presentation_relations 項目");
            string relationId = GetScalar(relation, "relation_id", contractPath);
            if (!relationIds.Add(relationId))
            {
                issues.Add(CreateIssue("PRES-RELATION-DUPLICATE", contractPath, $"presentation 全域關聯 ID 重複：{relationId}"));
            }

            ValidateValue(relation, "ownership", "reference_only", contractPath, issues);
            ValidateRelation(relation, contractPath, projectRoot, issues);
        }

        YamlMappingNode visibility = GetMapping(contract, "visibility", contractPath);
        HashSet<string> required = GetScalarSet(visibility, "required", contractPath);
        HashSet<string> forbidden = GetScalarSet(visibility, "forbidden", contractPath);
        List<string> overlap = required.Intersect(forbidden, StringComparer.Ordinal).ToList();
        if (overlap.Count > 0)
        {
            issues.Add(CreateIssue(
                "PRES-VISIBILITY-CONFLICT",
                contractPath,
                $"required 與 forbidden 不得重疊：{string.Join(", ", overlap)}"));
        }

        if (catalogScreenId == "game-over")
        {
            ValidateValue(screen, "board_mode", "none", contractPath, issues);
        }

        if (catalogScreenId == "game-completed")
        {
            ValidateValue(screen, "board_mode", "frozen", contractPath, issues);
        }

        ValidateStateVariants(catalogScreenId, contract, contractPath, issues);
    }

    private void ValidateStateVariants(
        string screenId,
        YamlMappingNode contract,
        string contractPath,
        List<GateIssue> issues)
    {
        bool requiresShieldRescue = screenId == "second-level-playing" || screenId == "third-level-playing";
        bool hasStateVariants = TryGetNode(contract, "state_variants", out YamlNode? variantsNode);
        if (!requiresShieldRescue)
        {
            if (hasStateVariants)
            {
                issues.Add(CreateIssue("PRES-VARIANT-UNEXPECTED", contractPath, $"{screenId} 不得宣告 shield rescue state variant。"));
            }

            return;
        }

        if (!hasStateVariants || variantsNode is not YamlSequenceNode variants || variants.Children.Count != 1)
        {
            issues.Add(CreateIssue("PRES-VARIANT-MISSING", contractPath, $"{screenId} 必須宣告唯一的 shield rescue state variant。"));
            return;
        }

        YamlMappingNode variant = RequireMapping(variants.Children[0], contractPath, "state_variants 項目");
        ValidateValue(variant, "variant_id", "shield-rescue", contractPath, issues);
        ValidateValue(variant, "trigger", "shield_rescue", contractPath, issues);
        HashSet<string> actualEffects = GetScalarSet(variant, "required_effects", contractPath);
        HashSet<string> expectedEffects = new HashSet<string>(StringComparer.Ordinal)
        {
            "shield-count-updated",
            "snake-restored-to-safe-start",
            "pre-hit-length-and-progress-preserved",
            "automatic-movement-resumed",
            "no-start-or-result-screen"
        };
        if (!actualEffects.SetEquals(expectedEffects))
        {
            issues.Add(CreateIssue("PRES-VARIANT-EFFECTS", contractPath, "shield rescue 顯示效果與 use case 不一致。"));
        }
    }

    private void ValidateRelation(
        YamlMappingNode relation,
        string contractPath,
        string projectRoot,
        List<GateIssue> issues)
    {
        string targetUseCase = GetScalar(relation, "target_use_case", contractPath);
        YamlMappingNode targetSpecs = GetMapping(relation, "target_specs", contractPath);
        string relationDirectory = Path.GetDirectoryName(contractPath)!;
        string framePath = ResolveProjectPath(relationDirectory, GetScalar(targetSpecs, "frame", contractPath), projectRoot, contractPath);
        string domainPath = ResolveProjectPath(relationDirectory, GetScalar(targetSpecs, "domain", contractPath), projectRoot, contractPath);
        string technicalPath = ResolveProjectPath(relationDirectory, GetScalar(targetSpecs, "technical", contractPath), projectRoot, contractPath);

        if (!ValidateSpecFilesExist(new List<string> { framePath, domainPath, technicalPath }, contractPath, issues))
        {
            return;
        }

        YamlMappingNode frame = LoadYaml(framePath);
        YamlMappingNode domain = LoadYaml(domainPath);
        YamlMappingNode technical = LoadYaml(technicalPath);
        ValidateUseCase(frame, targetUseCase, framePath, true, issues);
        ValidateUseCase(domain, targetUseCase, domainPath, false, issues);
        ValidateUseCase(technical, targetUseCase, technicalPath, false, issues);

        HashSet<string> frameRefs = GetScalarSet(relation, "frame_check_refs", contractPath);
        HashSet<string> domainRefs = GetScalarSet(relation, "domain_rule_refs", contractPath);
        HashSet<string> technicalRefs = GetScalarSet(relation, "technical_contract_refs", contractPath);
        ValidateRelationReferencesNotEmpty(frameRefs, domainRefs, technicalRefs, contractPath, issues);
        HashSet<string> frameChecks = CollectValuesForKey(frame, "check_id");
        YamlMappingNode traceability = GetMapping(domain, "traceability", domainPath);
        HashSet<string> domainFrameChecks = GetScalarSet(traceability, "frame_checks", domainPath);
        HashSet<string> domainRuleIds = CollectValuesForKey(domain, "rule_id");
        HashSet<string> technicalScalars = CollectScalars(technical);

        ValidateReferences(frameRefs, frameChecks, "PRES-FRAME-REF", framePath, issues);
        ValidateReferences(frameRefs, domainFrameChecks, "PRES-FRAME-DOMAIN-TRACE", domainPath, issues);
        ValidateReferences(domainRefs, domainRuleIds, "PRES-DOMAIN-REF", domainPath, issues);
        ValidateReferences(domainRefs, technicalScalars, "PRES-DOMAIN-TECHNICAL-TRACE", technicalPath, issues);
        ValidateReferences(technicalRefs, technicalScalars, "PRES-TECHNICAL-REF", technicalPath, issues);
    }

    private void ValidateTransitions(Dictionary<string, string> contractPaths, List<GateIssue> issues)
    {
        HashSet<string> actualTransitions = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, string> expectedTransitionUseCases = GetExpectedTransitionUseCases();
        foreach (KeyValuePair<string, string> contractEntry in contractPaths)
        {
            string screenId = contractEntry.Key;
            string contractPath = contractEntry.Value;
            YamlMappingNode contract = LoadYaml(contractPath);
            Dictionary<string, string> relationTargets = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (YamlNode relationNode in GetSequence(contract, "presentation_relations", contractPath).Children)
            {
                YamlMappingNode relation = RequireMapping(relationNode, contractPath, "presentation_relations 項目");
                relationTargets[GetScalar(relation, "relation_id", contractPath)] = GetScalar(relation, "target_use_case", contractPath);
            }

            YamlSequenceNode transitions = GetSequence(contract, "transitions", contractPath);
            foreach (YamlNode transitionNode in transitions.Children)
            {
                YamlMappingNode transition = RequireMapping(transitionNode, contractPath, "transitions 項目");
                string trigger = GetScalar(transition, "trigger", contractPath);
                string targetScreen = GetScalar(transition, "target_screen", contractPath);
                string relationRef = GetScalar(transition, "relation_ref", contractPath);
                if (!relationTargets.TryGetValue(relationRef, out string? relationTarget))
                {
                    issues.Add(CreateIssue(
                        "PRES-TRANSITION-RELATION",
                        contractPath,
                        $"畫面轉換 {trigger} 找不到同畫面的 relation：{relationRef}"));
                }

                string transitionKey = $"{screenId}|{trigger}|{targetScreen}";
                if (relationTarget != null
                    && expectedTransitionUseCases.TryGetValue(transitionKey, out string? expectedUseCase)
                    && relationTarget != expectedUseCase)
                {
                    issues.Add(CreateIssue(
                        "PRES-TRANSITION-USE-CASE",
                        contractPath,
                        $"畫面轉換 {transitionKey} 必須關聯 {expectedUseCase}，實際為 {relationTarget}。"));
                }

                if (!actualTransitions.Add(transitionKey))
                {
                    issues.Add(CreateIssue("PRES-TRANSITION-DUPLICATE", contractPath, $"畫面轉換重複：{transitionKey}"));
                }
            }
        }

        HashSet<string> expectedTransitions = expectedTransitionUseCases.Keys.ToHashSet(StringComparer.Ordinal);

        List<string> missing = expectedTransitions.Except(actualTransitions, StringComparer.Ordinal).ToList();
        List<string> unexpected = actualTransitions.Except(expectedTransitions, StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
        {
            issues.Add(CreateIssue("PRES-TRANSITION-MISSING", "presentation", $"缺少必要畫面轉換：{string.Join(", ", missing)}"));
        }

        if (unexpected.Count > 0)
        {
            issues.Add(CreateIssue("PRES-TRANSITION-UNEXPECTED", "presentation", $"出現未定義畫面轉換：{string.Join(", ", unexpected)}"));
        }
    }

    private Dictionary<string, string> GetExpectedTransitionUseCases()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["first-level-ready|start|first-level-playing"] = "activate-first-level-play",
            ["first-level-playing|level_passed|second-level-ready"] = "prepare-next-level",
            ["first-level-playing|death|game-over"] = "end-current-level-play",
            ["second-level-ready|start|second-level-playing"] = "activate-second-level-play",
            ["second-level-playing|level_passed|third-level-ready"] = "prepare-next-level",
            ["second-level-playing|death|game-over"] = "end-current-level-play",
            ["second-level-playing|shield_rescue|second-level-playing"] = "rescue-with-shield",
            ["third-level-ready|start|third-level-playing"] = "activate-third-level-play",
            ["third-level-playing|completed|game-completed"] = "end-current-level-play",
            ["third-level-playing|death|game-over"] = "end-current-level-play",
            ["third-level-playing|shield_rescue|third-level-playing"] = "rescue-with-shield",
            ["game-over|replay|first-level-playing"] = "reset-game-for-replay",
            ["game-completed|replay|first-level-playing"] = "reset-game-for-replay"
        };
    }

    private void ValidateAssetPairs(
        Dictionary<string, string> contractPaths,
        string projectRoot,
        List<GateIssue> issues)
    {
        Dictionary<string, string> expectedAssets = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["first-level-ready"] = "level-1",
            ["second-level-ready"] = "level-2",
            ["third-level-ready"] = "level-3"
        };

        foreach (KeyValuePair<string, string> contractEntry in contractPaths)
        {
            string contractPath = contractEntry.Value;
            YamlMappingNode contract = LoadYaml(contractPath);
            bool hasAssetPair = TryGetNode(contract, "asset_pair", out YamlNode? assetNode);
            if (!expectedAssets.TryGetValue(contractEntry.Key, out string? expectedBaseName))
            {
                if (hasAssetPair)
                {
                    issues.Add(CreateIssue("PRES-ASSET-UNEXPECTED", contractPath, $"{contractEntry.Key} 不得宣告預覽資產。"));
                }

                continue;
            }

            if (!hasAssetPair || assetNode is not YamlMappingNode assetPair)
            {
                issues.Add(CreateIssue("PRES-ASSET-MISSING", contractPath, $"{contractEntry.Key} 缺少 HTML/PNG 預覽。"));
                continue;
            }

            string contractDirectory = Path.GetDirectoryName(contractPath)!;
            string htmlPath = ResolveProjectPath(contractDirectory, GetScalar(assetPair, "html", contractPath), projectRoot, contractPath);
            string pngPath = ResolveProjectPath(contractDirectory, GetScalar(assetPair, "png", contractPath), projectRoot, contractPath);
            ValidateExpectedAssetPath(htmlPath, Path.Combine(projectRoot, "assets", $"{expectedBaseName}.html"), contractPath, issues);
            ValidateExpectedAssetPath(pngPath, Path.Combine(projectRoot, "assets", $"{expectedBaseName}.png"), contractPath, issues);
            ValidateAssetFile(htmlPath, contractPath, issues);
            ValidatePng(pngPath, contractPath, issues);
        }
    }

    private void ValidateNoReverseRelations(string domainsPath, string presentationPath, List<GateIssue> issues)
    {
        foreach (string domainSpecPath in Directory.GetFiles(domainsPath, "domain_spec.yaml", SearchOption.AllDirectories))
        {
            if (domainSpecPath.StartsWith(presentationPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            YamlMappingNode domainSpec = LoadYaml(domainSpecPath);
            if (ContainsPresentationReference(domainSpec))
            {
                issues.Add(CreateIssue(
                    "PRES-REVERSE-RELATION",
                    domainSpecPath,
                    "Domain 規格不得反向參照 presentation；presentation 僅能以 reference_only 指向既有契約。"));
            }
        }
    }

    private void ValidateExactScreens(
        Dictionary<string, string> expectedScreens,
        HashSet<string> actualScreens,
        string catalogPath,
        List<GateIssue> issues)
    {
        List<string> missing = expectedScreens.Keys.Except(actualScreens, StringComparer.Ordinal).ToList();
        List<string> unexpected = actualScreens.Except(expectedScreens.Keys, StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
        {
            issues.Add(CreateIssue("PRES-SCREEN-MISSING", catalogPath, $"缺少必要畫面：{string.Join(", ", missing)}"));
        }

        if (unexpected.Count > 0)
        {
            issues.Add(CreateIssue("PRES-SCREEN-UNEXPECTED", catalogPath, $"出現未定義畫面：{string.Join(", ", unexpected)}"));
        }
    }

    private Dictionary<string, string> GetExpectedScreens()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["first-level-ready"] = "first-level-ready/presentation.yaml",
            ["first-level-playing"] = "first-level-playing/presentation.yaml",
            ["second-level-ready"] = "second-level-ready/presentation.yaml",
            ["second-level-playing"] = "second-level-playing/presentation.yaml",
            ["third-level-ready"] = "third-level-ready/presentation.yaml",
            ["third-level-playing"] = "third-level-playing/presentation.yaml",
            ["game-over"] = "game-over/presentation.yaml",
            ["game-completed"] = "game-completed/presentation.yaml"
        };
    }

    private bool ValidateSpecFilesExist(List<string> paths, string contractPath, List<GateIssue> issues)
    {
        bool allExist = true;
        foreach (string path in paths)
        {
            if (!File.Exists(path))
            {
                issues.Add(CreateIssue("PRES-SPEC-NOT-FOUND", contractPath, $"找不到關聯規格：{path}"));
                allExist = false;
            }
        }

        return allExist;
    }

    private void ValidateUseCase(
        YamlMappingNode document,
        string expectedUseCase,
        string path,
        bool isFrame,
        List<GateIssue> issues)
    {
        string actualUseCase;
        if (isFrame)
        {
            YamlMappingNode useCase = GetMapping(document, "use_case", path);
            actualUseCase = GetScalar(useCase, "name", path);
        }
        else
        {
            actualUseCase = GetScalar(document, "use_case", path);
        }

        if (actualUseCase != expectedUseCase)
        {
            issues.Add(CreateIssue(
                "PRES-USE-CASE-MISMATCH",
                path,
                $"預期 use case {expectedUseCase}，實際為 {actualUseCase}。"));
        }
    }

    private void ValidateReferences(
        HashSet<string> references,
        HashSet<string> availableValues,
        string ruleId,
        string path,
        List<GateIssue> issues)
    {
        foreach (string reference in references)
        {
            if (!availableValues.Contains(reference))
            {
                issues.Add(CreateIssue(ruleId, path, $"找不到關聯 ID：{reference}"));
            }
        }
    }

    private void ValidateRelationReferencesNotEmpty(
        HashSet<string> frameRefs,
        HashSet<string> domainRefs,
        HashSet<string> technicalRefs,
        string path,
        List<GateIssue> issues)
    {
        if (frameRefs.Count == 0 || domainRefs.Count == 0 || technicalRefs.Count == 0)
        {
            issues.Add(CreateIssue(
                "PRES-RELATION-TRACE-MISSING",
                path,
                "每條 presentation relation 都必須包含 frame、domain 與 technical 追溯 ID。"));
        }
    }

    private void ValidateSchemaVersion(YamlMappingNode document, string path, List<GateIssue> issues)
    {
        ValidateValue(document, "schema_version", "1", path, issues);
    }

    private void ValidateValue(
        YamlMappingNode mapping,
        string key,
        string expected,
        string path,
        List<GateIssue> issues)
    {
        string actual = GetScalar(mapping, key, path);
        if (actual != expected)
        {
            issues.Add(CreateIssue("PRES-SCHEMA-VALUE", path, $"{key} 必須是 {expected}，實際為 {actual}。"));
        }
    }

    private void ValidateExpectedAssetPath(string path, string expectedPath, string contractPath, List<GateIssue> issues)
    {
        string fullExpectedPath = Path.GetFullPath(expectedPath);
        if (!string.Equals(path, fullExpectedPath, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(CreateIssue("PRES-ASSET-PATH", contractPath, $"預覽資產必須位於 {fullExpectedPath}，實際為 {path}。"));
        }
    }

    private void ValidateAssetFile(string path, string contractPath, List<GateIssue> issues)
    {
        if (!File.Exists(path))
        {
            issues.Add(CreateIssue("PRES-ASSET-NOT-FOUND", contractPath, $"找不到預覽資產：{path}"));
        }
    }

    private void ValidatePng(string path, string contractPath, List<GateIssue> issues)
    {
        if (!File.Exists(path))
        {
            issues.Add(CreateIssue("PRES-ASSET-NOT-FOUND", contractPath, $"找不到預覽資產：{path}"));
            return;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            byte[] header = new byte[24];
            int bytesRead = stream.Read(header, 0, header.Length);
            byte[] pngSignature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
            if (bytesRead < header.Length || !header.Take(8).SequenceEqual(pngSignature))
            {
                issues.Add(CreateIssue("PRES-ASSET-PNG", contractPath, $"不是有效的 PNG：{path}"));
                return;
            }

            int width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4));
            int height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4));
            if (width != 1536 || height != 1024)
            {
                issues.Add(CreateIssue("PRES-ASSET-DIMENSIONS", contractPath, $"PNG 必須是 1536×1024，實際為 {width}×{height}：{path}"));
            }
        }
        catch (Exception exception)
        {
            issues.Add(CreateIssue("PRES-ASSET-PNG", contractPath, $"讀取 PNG 失敗：{path}；{exception.Message}"));
        }
    }

    private string ResolveProjectPath(string basePath, string relativePath, string projectRoot, string sourcePath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(basePath, relativePath));
        string fullProjectRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string projectPrefix = $"{fullProjectRoot}{Path.DirectorySeparatorChar}";
        if (!string.Equals(fullPath, fullProjectRoot, StringComparison.OrdinalIgnoreCase)
            && !fullPath.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"{sourcePath} 的路徑超出專案根目錄：{relativePath}");
        }

        return fullPath;
    }

    private bool ContainsPresentationReference(YamlNode node)
    {
        if (node is YamlMappingNode mapping)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
            {
                string key = ((YamlScalarNode)entry.Key).Value ?? string.Empty;
                if (key == "presentation_refs" || key == "presentation_relations")
                {
                    return true;
                }

                if (ContainsPresentationReference(entry.Value))
                {
                    return true;
                }
            }
        }
        else if (node is YamlSequenceNode sequence)
        {
            foreach (YamlNode child in sequence.Children)
            {
                if (ContainsPresentationReference(child))
                {
                    return true;
                }
            }
        }
        else if (node is YamlScalarNode scalar)
        {
            string value = scalar.Value ?? string.Empty;
            return value.Contains("/presentation/", StringComparison.OrdinalIgnoreCase)
                || value.EndsWith("presentation.yaml", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private YamlMappingNode LoadYaml(string path)
    {
        try
        {
            using StreamReader reader = File.OpenText(path);
            YamlStream yaml = new YamlStream();
            yaml.Load(reader);
            if (yaml.Documents.Count != 1 || yaml.Documents[0].RootNode is not YamlMappingNode root)
            {
                throw new InvalidDataException("YAML 根節點必須是 mapping，且只能包含一份文件。");
            }

            return root;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"讀取 YAML 失敗：{path}；{exception.Message}", exception);
        }
    }

    private YamlMappingNode GetMapping(YamlMappingNode parent, string key, string path)
    {
        if (!TryGetNode(parent, key, out YamlNode? node) || node is not YamlMappingNode mapping)
        {
            throw new InvalidDataException($"{path} 缺少 mapping：{key}");
        }

        return mapping;
    }

    private YamlSequenceNode GetSequence(YamlMappingNode parent, string key, string path)
    {
        if (!TryGetNode(parent, key, out YamlNode? node) || node is not YamlSequenceNode sequence)
        {
            throw new InvalidDataException($"{path} 缺少 sequence：{key}");
        }

        return sequence;
    }

    private HashSet<string> GetScalarSet(YamlMappingNode parent, string key, string path)
    {
        YamlSequenceNode sequence = GetSequence(parent, key, path);
        HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);
        foreach (YamlNode child in sequence.Children)
        {
            if (child is not YamlScalarNode scalar || string.IsNullOrWhiteSpace(scalar.Value))
            {
                throw new InvalidDataException($"{path} 的 {key} 只能包含非空字串。");
            }

            if (!values.Add(scalar.Value))
            {
                throw new InvalidDataException($"{path} 的 {key} 包含重複值：{scalar.Value}");
            }
        }

        return values;
    }

    private string GetScalar(YamlMappingNode parent, string key, string path)
    {
        if (!TryGetNode(parent, key, out YamlNode? node)
            || node is not YamlScalarNode scalar
            || string.IsNullOrWhiteSpace(scalar.Value))
        {
            throw new InvalidDataException($"{path} 缺少非空字串：{key}");
        }

        return scalar.Value;
    }

    private bool TryGetNode(YamlMappingNode parent, string key, out YamlNode? value)
    {
        return parent.Children.TryGetValue(new YamlScalarNode(key), out value);
    }

    private YamlMappingNode RequireMapping(YamlNode node, string path, string description)
    {
        if (node is not YamlMappingNode mapping)
        {
            throw new InvalidDataException($"{path} 的 {description} 必須是 mapping。");
        }

        return mapping;
    }

    private HashSet<string> CollectScalars(YamlNode node)
    {
        HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);
        CollectScalars(node, values);
        return values;
    }

    private HashSet<string> CollectValuesForKey(YamlNode node, string key)
    {
        HashSet<string> values = new HashSet<string>(StringComparer.Ordinal);
        CollectValuesForKey(node, key, values);
        return values;
    }

    private void CollectValuesForKey(YamlNode node, string key, HashSet<string> values)
    {
        if (node is YamlSequenceNode sequence)
        {
            foreach (YamlNode child in sequence.Children)
            {
                CollectValuesForKey(child, key, values);
            }

            return;
        }

        if (node is not YamlMappingNode mapping)
        {
            return;
        }

        foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
        {
            string entryKey = (entry.Key as YamlScalarNode)?.Value ?? string.Empty;
            if (entryKey == key && entry.Value is YamlScalarNode scalar && !string.IsNullOrWhiteSpace(scalar.Value))
            {
                values.Add(scalar.Value);
            }

            CollectValuesForKey(entry.Value, key, values);
        }
    }

    private void CollectScalars(YamlNode node, HashSet<string> values)
    {
        if (node is YamlScalarNode scalar)
        {
            if (!string.IsNullOrWhiteSpace(scalar.Value))
            {
                values.Add(scalar.Value);
            }

            return;
        }

        if (node is YamlSequenceNode sequence)
        {
            foreach (YamlNode child in sequence.Children)
            {
                CollectScalars(child, values);
            }

            return;
        }

        if (node is YamlMappingNode mapping)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
            {
                CollectScalars(entry.Key, values);
                CollectScalars(entry.Value, values);
            }
        }
    }

    private GateIssue CreateIssue(string ruleId, string file, string message)
    {
        return new GateIssue(ruleId, "presentation", file, null, message, "error");
    }
}
