using System.Buffers.Binary;
using YamlDotNet.RepresentationModel;

namespace PatternNetwork.Gates;

public class PresentationContractValidator
{
    private const string RenderWorkflowId = "V3-PRES-WF-GAME-RENDER-SCREEN";
    private const string RenderAcceptanceId = "V3-ACCEPT-GAME-RENDER-SCREEN";
    private const string DirectionInputWorkflowId = "V3-PRES-WF-GAME-HANDLE-DIRECTION-INPUT";
    private const string DirectionInputAcceptanceId = "V3-ACCEPT-GAME-HANDLE-DIRECTION-INPUT";
    private const string SetSnakeDirectionTechnicalId = "V3-TECH-GAME-SET-SNAKE-DIRECTION";
    private const string GetGameScreenTechnicalId = "V3-TECH-GAME-GET-GAME-SCREEN";
    private const string GetGameScreenDomainId = "V3-DOMAIN-GAME-GET-GAME-SCREEN";

    public PresentationContractValidationResult Validate(string domainsPath, string projectRoot)
    {
        List<GateIssue> issues = new List<GateIssue>();
        if (!Directory.Exists(domainsPath))
        {
            issues.Add(CreateIssue("PRES-CONFIG-DOMAINS", domainsPath, $"找不到 Domains 資料夾：{domainsPath}"));
            return new PresentationContractValidationResult(issues, true);
        }

        if (!Directory.Exists(projectRoot))
        {
            issues.Add(CreateIssue("PRES-CONFIG-PROJECT", projectRoot, $"找不到專案根目錄：{projectRoot}"));
            return new PresentationContractValidationResult(issues, true);
        }

        string fullDomainsPath = Path.GetFullPath(domainsPath);
        string fullProjectRoot = Path.GetFullPath(projectRoot);
        string catalogPath = Path.Combine(fullDomainsPath, "catalog.yaml");
        if (!File.Exists(catalogPath))
        {
            issues.Add(CreateIssue("PRES-CONFIG-CATALOG", catalogPath, "找不到 Domains/catalog.yaml。"));
            return new PresentationContractValidationResult(issues, true);
        }

        SpecificationValidator specificationValidator = new SpecificationValidator();
        SpecificationValidationResult specificationResult = specificationValidator.Validate(
            fullDomainsPath,
            new List<string> { "catalog.yaml" });
        issues.AddRange(specificationResult.Issues);

        try
        {
            Dictionary<string, PresentationStateDefinition> expectedStates = GetExpectedStates();
            YamlMappingNode catalog = LoadYaml(catalogPath);
            List<string> catalogStateIds = ReadScalarList(
                catalog,
                "presentation_states",
                catalogPath,
                "PRES-CATALOG-STATES",
                issues);
            ValidateExactSet(
                catalogStateIds,
                expectedStates.Keys,
                "PRES-CATALOG-STATES",
                catalogPath,
                "active catalog 的 presentation_states 必須恰好包含八個遊戲畫面",
                issues);

            List<string> catalogWorkflowIds = ReadScalarList(
                catalog,
                "presentation_workflows",
                catalogPath,
                "PRES-CATALOG-WORKFLOWS",
                issues);
            ValidateExactSet(
                catalogWorkflowIds,
                new[] { RenderWorkflowId, DirectionInputWorkflowId },
                "PRES-CATALOG-WORKFLOWS",
                catalogPath,
                "active catalog 必須恰好列出 RenderGameScreen 與 HandleDirectionInput presentation workflows",
                issues);

            string? workflowPath = FindDocumentPath(specificationResult, RenderWorkflowId);
            if (workflowPath is null)
            {
                issues.Add(CreateIssue(
                    "PRES-RENDER-MISSING",
                    catalogPath,
                    $"找不到 {RenderWorkflowId}。"));
            }
            else
            {
                ValidateRenderWorkflow(workflowPath, expectedStates.Keys, issues);
            }

            string? directionInputWorkflowPath = FindDocumentPath(
                specificationResult,
                DirectionInputWorkflowId);
            if (directionInputWorkflowPath is null)
            {
                issues.Add(CreateIssue(
                    "PRES-DIRECTION-MISSING",
                    catalogPath,
                    $"找不到 {DirectionInputWorkflowId}。"));
            }
            else
            {
                ValidateDirectionInputWorkflow(directionInputWorkflowPath, issues);
            }

            foreach (KeyValuePair<string, PresentationStateDefinition> expectedState in expectedStates)
            {
                string? statePath = FindDocumentPath(specificationResult, expectedState.Key);
                if (statePath is null)
                {
                    issues.Add(CreateIssue(
                        "PRES-STATE-MISSING",
                        catalogPath,
                        $"找不到 presentation state：{expectedState.Key}。"));
                    continue;
                }

                string expectedPath = Path.GetFullPath(Path.Combine(fullDomainsPath, expectedState.Value.RelativePath));
                if (!string.Equals(statePath, expectedPath, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(CreateIssue(
                        "PRES-STATE-PATH",
                        statePath,
                        $"{expectedState.Key} 必須位於 {expectedPath}。"));
                }

                ValidateState(
                    expectedState.Key,
                    expectedState.Value,
                    statePath,
                    expectedStates.Keys.ToHashSet(StringComparer.Ordinal),
                    fullProjectRoot,
                    issues);
            }

            ValidateNoReverseRelations(specificationResult, issues);
        }
        catch (Exception exception)
        {
            issues.Add(CreateIssue(
                "PRES-CONFIG-YAML",
                catalogPath,
                $"讀取 presentation YAML 失敗：{exception.Message}"));
            return new PresentationContractValidationResult(issues, true);
        }

        return new PresentationContractValidationResult(issues, specificationResult.HasConfigurationError);
    }

    private void ValidateRenderWorkflow(
        string workflowPath,
        IEnumerable<string> expectedStateIds,
        List<GateIssue> issues)
    {
        YamlMappingNode workflow = LoadYaml(workflowPath);
        ValidateScalarValue(
            workflow,
            "workflow",
            "RenderGameScreen",
            workflowPath,
            "PRES-RENDER-WORKFLOW",
            issues);
        ValidateScalarValue(
            workflow,
            "technical_ref",
            GetGameScreenTechnicalId,
            workflowPath,
            "PRES-RENDER-WORKFLOW",
            issues);

        List<string> stateRefs = ReadScalarList(
            workflow,
            "state_refs",
            workflowPath,
            "PRES-RENDER-STATES",
            issues);
        ValidateExactSet(
            stateRefs,
            expectedStateIds,
            "PRES-RENDER-STATES",
            workflowPath,
            "RenderGameScreen.state_refs 必須恰好包含八個 active presentation states",
            issues);

        YamlMappingNode? input = ReadMapping(workflow, "input", workflowPath, "PRES-RENDER-GRAIN", issues);
        if (input is not null)
        {
            ValidateScalarValue(input, "model", "GameScreenDTO", workflowPath, "PRES-RENDER-GRAIN", issues);
            ValidateScalarValue(input, "grain", "one_complete_game_screen", workflowPath, "PRES-RENDER-GRAIN", issues);
            ValidateScalarValue(input, "owner", "GetGameScreenUseCase", workflowPath, "PRES-RENDER-GRAIN", issues);
        }

        YamlMappingNode? output = ReadMapping(workflow, "output", workflowPath, "PRES-RENDER-GRAIN", issues);
        if (output is not null)
        {
            ValidateScalarValue(output, "grain", "changed_ui_controls_and_board_cells", workflowPath, "PRES-RENDER-GRAIN", issues);
            ValidateScalarValue(output, "owner", "GameForm", workflowPath, "PRES-RENDER-GRAIN", issues);
        }

        YamlMappingNode? renderPolicy = ReadMapping(
            workflow,
            "render_policy",
            workflowPath,
            "PRES-RENDER-POLICY",
            issues);
        if (renderPolicy is not null)
        {
            Dictionary<string, string> expectedRenderPolicy = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["owner"] = "GameForm",
                ["method"] = "ApplyScreen",
                ["board_update"] = "create_once_then_update_changed_cells",
                ["visibility"] = "update_only_when_value_changes"
            };
            foreach (KeyValuePair<string, string> policy in expectedRenderPolicy)
            {
                ValidateScalarValue(
                    renderPolicy,
                    policy.Key,
                    policy.Value,
                    workflowPath,
                    "PRES-RENDER-POLICY",
                    issues);
            }
        }

        YamlMappingNode? runtimePolicy = ReadMapping(
            workflow,
            "runtime_policy",
            workflowPath,
            "PRES-RUNTIME-POLICY",
            issues);
        if (runtimePolicy is not null)
        {
            Dictionary<string, string> expectedRuntimePolicy = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["event_owner"] = "GameForm",
                ["use_case_order_owner"] = "GameForm",
                ["timer_interval_ms"] = "250",
                ["move_calls_per_tick"] = "1",
                ["accumulated_time_allowed"] = "false",
                ["catch_up_loop_allowed"] = "false"
            };
            foreach (KeyValuePair<string, string> policy in expectedRuntimePolicy)
            {
                ValidateScalarValue(
                    runtimePolicy,
                    policy.Key,
                    policy.Value,
                    workflowPath,
                    "PRES-RUNTIME-POLICY",
                    issues);
            }

            ValidateExactSet(
                ReadScalarList(
                    runtimePolicy,
                    "forbidden_roles",
                    workflowPath,
                    "PRES-RUNTIME-POLICY",
                    issues),
                new[]
                {
                    "Presenter",
                    "IGameView",
                    "Controller",
                    "GameExecutor",
                    "GameRender",
                    "Renderer",
                    "ViewModel",
                    "Display",
                    "custom_Control"
                },
                "PRES-RUNTIME-POLICY",
                workflowPath,
                "UI 的禁止角色集合不完整",
                issues);
        }

        ValidateScalarValue(
            workflow,
            "acceptance_ref",
            RenderAcceptanceId,
            workflowPath,
            "PRES-RENDER-ACCEPTANCE",
            issues);
    }

    private void ValidateDirectionInputWorkflow(
        string workflowPath,
        List<GateIssue> issues)
    {
        YamlMappingNode workflow = LoadYaml(workflowPath);
        ValidateScalarValue(
            workflow,
            "workflow",
            "HandleDirectionInput",
            workflowPath,
            "PRES-DIRECTION-WORKFLOW",
            issues);
        ValidateScalarValue(
            workflow,
            "technical_ref",
            SetSnakeDirectionTechnicalId,
            workflowPath,
            "PRES-DIRECTION-WORKFLOW",
            issues);

        YamlMappingNode? input = ReadMapping(
            workflow,
            "input",
            workflowPath,
            "PRES-DIRECTION-GRAIN",
            issues);
        if (input is not null)
        {
            ValidateScalarValue(
                input,
                "grain",
                "one_direction_key_message",
                workflowPath,
                "PRES-DIRECTION-GRAIN",
                issues);
            ValidateScalarValue(
                input,
                "owner",
                "GameForm",
                workflowPath,
                "PRES-DIRECTION-GRAIN",
                issues);
        }

        YamlMappingNode? output = ReadMapping(
            workflow,
            "output",
            workflowPath,
            "PRES-DIRECTION-GRAIN",
            issues);
        if (output is not null)
        {
            ValidateScalarValue(
                output,
                "model",
                "SnakeDirectionDTO",
                workflowPath,
                "PRES-DIRECTION-GRAIN",
                issues);
            ValidateScalarValue(
                output,
                "grain",
                "one_direction_request",
                workflowPath,
                "PRES-DIRECTION-GRAIN",
                issues);
            ValidateScalarValue(
                output,
                "owner",
                "GameForm",
                workflowPath,
                "PRES-DIRECTION-GRAIN",
                issues);
        }

        YamlMappingNode? runtimePolicy = ReadMapping(
            workflow,
            "runtime_policy",
            workflowPath,
            "PRES-DIRECTION-DELIVERY",
            issues);
        if (runtimePolicy is not null)
        {
            Dictionary<string, string> expectedRuntimePolicy = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["delivery_stage"] = "ProcessCmdKey",
                ["focus_independent"] = "true",
                ["dispatch_count"] = "exactly_once",
                ["parallel_key_down_path_allowed"] = "false"
            };
            foreach (KeyValuePair<string, string> policy in expectedRuntimePolicy)
            {
                ValidateScalarValue(
                    runtimePolicy,
                    policy.Key,
                    policy.Value,
                    workflowPath,
                    "PRES-DIRECTION-DELIVERY",
                    issues);
            }

            YamlMappingNode? directionMapping = ReadMapping(
                runtimePolicy,
                "direction_mapping",
                workflowPath,
                "PRES-DIRECTION-MAPPING",
                issues);
            if (directionMapping is not null)
            {
                ValidateDirectionVectorMapping(directionMapping, workflowPath, issues);
            }
        }

        ValidateScalarValue(
            workflow,
            "acceptance_ref",
            DirectionInputAcceptanceId,
            workflowPath,
            "PRES-DIRECTION-ACCEPTANCE",
            issues);
    }

    private void ValidateState(
        string stateId,
        PresentationStateDefinition definition,
        string statePath,
        HashSet<string> expectedStateIds,
        string projectRoot,
        List<GateIssue> issues)
    {
        YamlMappingNode state = LoadYaml(statePath);
        ValidateScalarValue(state, "screen", definition.Screen, statePath, "PRES-STATE-SCREEN", issues);
        ValidateScalarValue(state, "grain", "atomic_rendered_screen", statePath, "PRES-STATE-GRAIN", issues);

        if (ContainsAnyKey(
            state,
            new HashSet<string>(StringComparer.Ordinal)
            {
                "presentation_relations",
                "target_use_case",
                "target_specs",
                "frame_check_refs",
                "domain_rule_refs",
                "technical_contract_refs",
                "relation_ref",
                "target_screen",
                "state_variants"
            }))
        {
            issues.Add(CreateIssue(
                "PRES-LEGACY-RELATION",
                statePath,
                "schema v3 presentation state 不得保留 leaf use case 或 v1 relation 欄位。"));
        }

        List<YamlMappingNode> relations = ReadMappingList(
            state,
            "relations",
            statePath,
            "PRES-RELATIONS",
            issues);
        List<YamlMappingNode> renderedByRelations = relations
            .Where(relation => ReadScalar(relation, "kind") == "rendered_by")
            .ToList();
        if (renderedByRelations.Count != 1
            || ReadScalar(renderedByRelations[0], "target_ref") != RenderWorkflowId)
        {
            issues.Add(CreateIssue(
                "PRES-RENDER-RELATION",
                statePath,
                $"{stateId} 必須由唯一的 {RenderWorkflowId} 渲染。"));
        }

        foreach (YamlMappingNode relation in relations.Where(
            relation => ReadScalar(relation, "kind") == "application_output"))
        {
            ValidateScalarValue(
                relation,
                "target_ref",
                GetGameScreenDomainId,
                statePath,
                "PRES-APPLICATION-OUTPUT",
                issues);
        }

        YamlMappingNode? visibility = ReadMapping(
            state,
            "visibility",
            statePath,
            "PRES-VISIBILITY",
            issues);
        if (visibility is not null)
        {
            HashSet<string> required = ReadScalarList(
                    visibility,
                    "required",
                    statePath,
                    "PRES-VISIBILITY",
                    issues)
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> forbidden = ReadScalarList(
                    visibility,
                    "forbidden",
                    statePath,
                    "PRES-VISIBILITY",
                    issues)
                .ToHashSet(StringComparer.Ordinal);
            List<string> overlap = required.Intersect(forbidden, StringComparer.Ordinal).ToList();
            if (overlap.Count > 0)
            {
                issues.Add(CreateIssue(
                    "PRES-VISIBILITY-CONFLICT",
                    statePath,
                    $"required 與 forbidden 不得重疊：{string.Join(", ", overlap)}"));
            }
        }

        List<YamlMappingNode> transitions = ReadMappingList(
            state,
            "transitions",
            statePath,
            "PRES-TRANSITIONS",
            issues);
        HashSet<string> transitionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (YamlMappingNode transition in transitions)
        {
            string? transitionId = ReadScalar(transition, "id");
            if (string.IsNullOrWhiteSpace(transitionId) || !transitionIds.Add(transitionId))
            {
                issues.Add(CreateIssue(
                    "PRES-TRANSITION-ID",
                    statePath,
                    "每個 transition 必須有唯一且非空的 id。"));
            }

            string? targetStateRef = ReadScalar(transition, "target_state_ref");
            if (string.IsNullOrWhiteSpace(targetStateRef) || !expectedStateIds.Contains(targetStateRef))
            {
                issues.Add(CreateIssue(
                    "PRES-TRANSITION-TARGET",
                    statePath,
                    $"transition {transitionId ?? "<missing>"} 引用非 active presentation state：{targetStateRef ?? "<missing>"}。"));
            }

            if (string.IsNullOrWhiteSpace(ReadScalar(transition, "trigger")))
            {
                issues.Add(CreateIssue(
                    "PRES-TRANSITION-TRIGGER",
                    statePath,
                    $"transition {transitionId ?? "<missing>"} 缺少 trigger。"));
            }
        }

        ValidateVisualAcceptance(
            stateId,
            definition.AssetBaseName,
            relations,
            statePath,
            projectRoot,
            issues);
    }

    private void ValidateVisualAcceptance(
        string stateId,
        string? assetBaseName,
        List<YamlMappingNode> relations,
        string statePath,
        string projectRoot,
        List<GateIssue> issues)
    {
        List<YamlMappingNode> assetRelations = relations
            .Where(relation => ReadScalar(relation, "kind") == "visual_acceptance")
            .ToList();
        if (assetBaseName is null)
        {
            if (assetRelations.Count > 0)
            {
                issues.Add(CreateIssue(
                    "PRES-ASSET-UNEXPECTED",
                    statePath,
                    $"{stateId} 不得宣告 Ready 畫面的預覽資產。"));
            }

            return;
        }

        if (assetRelations.Count != 1)
        {
            issues.Add(CreateIssue(
                "PRES-ASSET-MISSING",
                statePath,
                $"{stateId} 必須宣告唯一的 HTML/PNG visual_acceptance relation。"));
            return;
        }

        YamlMappingNode assetRelation = assetRelations[0];
        string? htmlRelativePath = ReadScalar(assetRelation, "html");
        string? pngRelativePath = ReadScalar(assetRelation, "png");
        if (string.IsNullOrWhiteSpace(htmlRelativePath) || string.IsNullOrWhiteSpace(pngRelativePath))
        {
            issues.Add(CreateIssue(
                "PRES-ASSET-MISSING",
                statePath,
                $"{stateId} 的 visual_acceptance 必須同時提供 html 與 png。"));
            return;
        }

        string stateDirectory = Path.GetDirectoryName(statePath)!;
        string htmlPath = ResolveProjectPath(stateDirectory, htmlRelativePath, projectRoot, statePath);
        string pngPath = ResolveProjectPath(stateDirectory, pngRelativePath, projectRoot, statePath);
        ValidateExpectedAssetPath(
            htmlPath,
            Path.Combine(projectRoot, "assets", $"{assetBaseName}.html"),
            statePath,
            issues);
        ValidateExpectedAssetPath(
            pngPath,
            Path.Combine(projectRoot, "assets", $"{assetBaseName}.png"),
            statePath,
            issues);
        ValidateAssetFile(htmlPath, statePath, issues);
        ValidatePng(pngPath, statePath, issues);
    }

    private void ValidateNoReverseRelations(
        SpecificationValidationResult specificationResult,
        List<GateIssue> issues)
    {
        foreach (SpecificationDocument aggregate in specificationResult.Inventory.Documents.Where(
            document => document.Header.SpecificationKind == SpecificationKind.AggregateContract))
        {
            YamlMappingNode root = LoadYaml(aggregate.FilePath);
            if (ContainsPresentationReference(root))
            {
                issues.Add(CreateIssue(
                    "PRES-REVERSE-RELATION",
                    aggregate.FilePath,
                    "Aggregate 契約不得反向依賴 presentation；關聯只能由 presentation 指向 Domain/Application contract。"));
            }
        }
    }

    private Dictionary<string, PresentationStateDefinition> GetExpectedStates()
    {
        return new Dictionary<string, PresentationStateDefinition>(StringComparer.Ordinal)
        {
            ["V3-PRES-STATE-GAME-FIRST-LEVEL-READY"] = new PresentationStateDefinition(
                "FirstLevelReady",
                Path.Combine("Game", "presentation", "first-level-ready", "presentation.yaml"),
                "level-1"),
            ["V3-PRES-STATE-GAME-FIRST-LEVEL-PLAYING"] = new PresentationStateDefinition(
                "FirstLevelPlaying",
                Path.Combine("Game", "presentation", "first-level-playing", "presentation.yaml"),
                null),
            ["V3-PRES-STATE-GAME-SECOND-LEVEL-READY"] = new PresentationStateDefinition(
                "SecondLevelReady",
                Path.Combine("Game", "presentation", "second-level-ready", "presentation.yaml"),
                "level-2"),
            ["V3-PRES-STATE-GAME-SECOND-LEVEL-PLAYING"] = new PresentationStateDefinition(
                "SecondLevelPlaying",
                Path.Combine("Game", "presentation", "second-level-playing", "presentation.yaml"),
                null),
            ["V3-PRES-STATE-GAME-THIRD-LEVEL-READY"] = new PresentationStateDefinition(
                "ThirdLevelReady",
                Path.Combine("Game", "presentation", "third-level-ready", "presentation.yaml"),
                "level-3"),
            ["V3-PRES-STATE-GAME-THIRD-LEVEL-PLAYING"] = new PresentationStateDefinition(
                "ThirdLevelPlaying",
                Path.Combine("Game", "presentation", "third-level-playing", "presentation.yaml"),
                null),
            ["V3-PRES-STATE-GAME-GAME-OVER"] = new PresentationStateDefinition(
                "GameOver",
                Path.Combine("Game", "presentation", "game-over", "presentation.yaml"),
                null),
            ["V3-PRES-STATE-GAME-GAME-COMPLETED"] = new PresentationStateDefinition(
                "GameCompleted",
                Path.Combine("Game", "presentation", "game-completed", "presentation.yaml"),
                null)
        };
    }

    private string? FindDocumentPath(SpecificationValidationResult result, string id)
    {
        return result.Inventory.Documents
            .FirstOrDefault(document => document.Header.Id == id)
            ?.FilePath;
    }

    private void ValidateExactSet(
        List<string> actual,
        IEnumerable<string> expectedValues,
        string ruleId,
        string path,
        string description,
        List<GateIssue> issues)
    {
        HashSet<string> expected = expectedValues.ToHashSet(StringComparer.Ordinal);
        HashSet<string> actualSet = actual.ToHashSet(StringComparer.Ordinal);
        if (actual.Count != actualSet.Count || !actualSet.SetEquals(expected))
        {
            List<string> missing = expected.Except(actualSet, StringComparer.Ordinal).Order().ToList();
            List<string> unexpected = actualSet.Except(expected, StringComparer.Ordinal).Order().ToList();
            issues.Add(CreateIssue(
                ruleId,
                path,
                $"{description}。缺少：{FormatValues(missing)}；多餘：{FormatValues(unexpected)}。"));
        }
    }

    private void ValidateExactSequence(
        List<string> actual,
        IEnumerable<string> expectedValues,
        string ruleId,
        string path,
        string description,
        List<GateIssue> issues)
    {
        List<string> expected = expectedValues.ToList();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            issues.Add(CreateIssue(
                ruleId,
                path,
                $"{description}。預期：{string.Join(" -> ", expected)}；實際：{string.Join(" -> ", actual)}。"));
        }
    }

    private void ValidateDirectionVectorMapping(
        YamlMappingNode directionMapping,
        string path,
        List<GateIssue> issues)
    {
        Dictionary<string, string> expectedVectors = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Up"] = "0,-1",
            ["Down"] = "0,1",
            ["Left"] = "-1,0",
            ["Right"] = "1,0"
        };
        HashSet<string> actualKeys = directionMapping.Children.Keys
            .OfType<YamlScalarNode>()
            .Where(key => !string.IsNullOrWhiteSpace(key.Value))
            .Select(key => key.Value!)
            .ToHashSet(StringComparer.Ordinal);

        if (!actualKeys.SetEquals(expectedVectors.Keys))
        {
            issues.Add(CreateIssue(
                "PRES-DIRECTION-MAPPING",
                path,
                "direction_mapping 必須恰好包含 Up、Down、Left、Right。"));
            return;
        }

        foreach (KeyValuePair<string, string> expectedVector in expectedVectors)
        {
            if (!directionMapping.Children.TryGetValue(
                    new YamlScalarNode(expectedVector.Key),
                    out YamlNode? vectorNode)
                || vectorNode is not YamlMappingNode vector
                || $"{ReadScalar(vector, "x")},{ReadScalar(vector, "y")}" != expectedVector.Value)
            {
                issues.Add(CreateIssue(
                    "PRES-DIRECTION-MAPPING",
                    path,
                    $"{expectedVector.Key} 必須映射到向量 {expectedVector.Value}。"));
            }
        }
    }

    private string FormatValues(List<string> values)
    {
        return values.Count == 0 ? "無" : string.Join(", ", values);
    }

    private void ValidateScalarValue(
        YamlMappingNode parent,
        string key,
        string expected,
        string path,
        string ruleId,
        List<GateIssue> issues)
    {
        string? actual = ReadScalar(parent, key);
        if (actual != expected)
        {
            issues.Add(CreateIssue(
                ruleId,
                path,
                $"{key} 必須是 {expected}，實際為 {actual ?? "<missing>"}。"));
        }
    }

    private YamlMappingNode? ReadMapping(
        YamlMappingNode parent,
        string key,
        string path,
        string ruleId,
        List<GateIssue> issues)
    {
        if (parent.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? node)
            && node is YamlMappingNode mapping)
        {
            return mapping;
        }

        issues.Add(CreateIssue(ruleId, path, $"{key} 必須是 mapping。"));
        return null;
    }

    private List<string> ReadScalarList(
        YamlMappingNode parent,
        string key,
        string path,
        string ruleId,
        List<GateIssue> issues)
    {
        if (!parent.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? node)
            || node is not YamlSequenceNode sequence)
        {
            issues.Add(CreateIssue(ruleId, path, $"{key} 必須是 scalar 清單。"));
            return new List<string>();
        }

        List<string> values = new List<string>();
        foreach (YamlNode child in sequence.Children)
        {
            if (child is not YamlScalarNode scalar || string.IsNullOrWhiteSpace(scalar.Value))
            {
                issues.Add(CreateIssue(ruleId, path, $"{key} 的每個項目都必須是非空 scalar。"));
                continue;
            }

            values.Add(scalar.Value);
        }

        return values;
    }

    private List<YamlMappingNode> ReadMappingList(
        YamlMappingNode parent,
        string key,
        string path,
        string ruleId,
        List<GateIssue> issues)
    {
        if (!parent.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? node)
            || node is not YamlSequenceNode sequence)
        {
            issues.Add(CreateIssue(ruleId, path, $"{key} 必須是 mapping 清單。"));
            return new List<YamlMappingNode>();
        }

        List<YamlMappingNode> mappings = new List<YamlMappingNode>();
        foreach (YamlNode child in sequence.Children)
        {
            if (child is not YamlMappingNode mapping)
            {
                issues.Add(CreateIssue(ruleId, path, $"{key} 的每個項目都必須是 mapping。"));
                continue;
            }

            mappings.Add(mapping);
        }

        return mappings;
    }

    private string? ReadScalar(YamlMappingNode parent, string key)
    {
        if (parent.Children.TryGetValue(new YamlScalarNode(key), out YamlNode? node)
            && node is YamlScalarNode scalar)
        {
            return scalar.Value;
        }

        return null;
    }

    private bool ContainsAnyKey(YamlNode node, HashSet<string> keys)
    {
        if (node is YamlMappingNode mapping)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
            {
                if (entry.Key is YamlScalarNode scalar
                    && !string.IsNullOrWhiteSpace(scalar.Value)
                    && keys.Contains(scalar.Value))
                {
                    return true;
                }

                if (ContainsAnyKey(entry.Value, keys))
                {
                    return true;
                }
            }
        }
        else if (node is YamlSequenceNode sequence)
        {
            foreach (YamlNode child in sequence.Children)
            {
                if (ContainsAnyKey(child, keys))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool ContainsPresentationReference(YamlNode node)
    {
        if (node is YamlMappingNode mapping)
        {
            foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
            {
                if (entry.Key is YamlScalarNode key
                    && (key.Value == "presentation_refs" || key.Value == "presentation_relations"))
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
            return value.StartsWith("V3-PRES-", StringComparison.Ordinal)
                || value.Contains("/presentation/", StringComparison.OrdinalIgnoreCase)
                || value.EndsWith("presentation.yaml", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private void ValidateExpectedAssetPath(
        string path,
        string expectedPath,
        string contractPath,
        List<GateIssue> issues)
    {
        string fullExpectedPath = Path.GetFullPath(expectedPath);
        if (!string.Equals(path, fullExpectedPath, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(CreateIssue(
                "PRES-ASSET-PATH",
                contractPath,
                $"預覽資產必須位於 {fullExpectedPath}，實際為 {path}。"));
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
                issues.Add(CreateIssue(
                    "PRES-ASSET-DIMENSIONS",
                    contractPath,
                    $"PNG 必須是 1536×1024，實際為 {width}×{height}：{path}"));
            }
        }
        catch (Exception exception)
        {
            issues.Add(CreateIssue(
                "PRES-ASSET-PNG",
                contractPath,
                $"讀取 PNG 失敗：{path}；{exception.Message}"));
        }
    }

    private string ResolveProjectPath(
        string basePath,
        string relativePath,
        string projectRoot,
        string sourcePath)
    {
        string fullPath = Path.GetFullPath(Path.Combine(basePath, relativePath));
        string fullProjectRoot = Path.GetFullPath(projectRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string projectPrefix = $"{fullProjectRoot}{Path.DirectorySeparatorChar}";
        if (!string.Equals(fullPath, fullProjectRoot, StringComparison.OrdinalIgnoreCase)
            && !fullPath.StartsWith(projectPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"{sourcePath} 的路徑超出專案根目錄：{relativePath}");
        }

        return fullPath;
    }

    private YamlMappingNode LoadYaml(string path)
    {
        using StreamReader reader = File.OpenText(path);
        YamlStream yaml = new YamlStream();
        yaml.Load(reader);
        if (yaml.Documents.Count != 1 || yaml.Documents[0].RootNode is not YamlMappingNode root)
        {
            throw new InvalidDataException($"{path} 必須只有一個 YAML mapping 文件。");
        }

        return root;
    }

    private GateIssue CreateIssue(string ruleId, string file, string message)
    {
        return new GateIssue(ruleId, "presentation", file, null, message, "error");
    }

    private sealed record PresentationStateDefinition(
        string Screen,
        string RelativePath,
        string? AssetBaseName);
}
