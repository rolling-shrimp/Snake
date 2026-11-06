using System.Text;
using System.Text.Json;
using YamlDotNet.RepresentationModel;

namespace PatternNetwork.Gates;

public class ProjectVerificationRunner
{
    public GateRunResult Verify(
        string networkPath,
        string domainsPath,
        string misfitPath,
        string solutionPath,
        string projectRoot,
        string outputDirectory)
    {
        List<GateCheckResult> checks = new List<GateCheckResult>();
        bool hasConfigurationError = false;

        PatternNetworkValidator networkValidator = new PatternNetworkValidator();
        PatternNetworkValidationResult networkValidation = networkValidator.Validate(networkPath);
        checks.Add(CreateBlockingCheck("verify-network", networkValidation.Issues));
        hasConfigurationError = hasConfigurationError || !networkValidation.IsValid;

        SpecificationValidator specificationValidator = new SpecificationValidator();
        SpecificationValidationResult specificationValidation = specificationValidator.Validate(domainsPath);
        checks.Add(CreateBlockingCheck("verify-specifications", specificationValidation.Issues));
        hasConfigurationError = hasConfigurationError || specificationValidation.HasConfigurationError;

        MigrationMapValidator migrationValidator = new MigrationMapValidator();
        MigrationMapValidationResult migrationValidation = migrationValidator.Validate(domainsPath, networkPath);
        checks.Add(CreateBlockingCheck("verify-migration", migrationValidation.Issues));
        hasConfigurationError = hasConfigurationError || migrationValidation.HasConfigurationError;

        PresentationContractValidator presentationValidator = new PresentationContractValidator();
        PresentationContractValidationResult presentationValidation = presentationValidator.Validate(
            domainsPath,
            projectRoot);
        checks.Add(CreateBlockingCheck("verify-presentations", presentationValidation.Issues));
        hasConfigurationError = hasConfigurationError || presentationValidation.HasConfigurationError;

        if (networkValidation.IsValid)
        {
            CodeGateRunner codeGateRunner = new CodeGateRunner();
            GateRunResult codeResult = codeGateRunner.CheckCode(networkPath, solutionPath, null);
            checks.AddRange(codeResult.Checks);
            hasConfigurationError = hasConfigurationError || codeResult.HasConfigurationError;
        }

        GateCheckerRegistry checkerRegistry = CreateCheckerRegistry(projectRoot);
        hasConfigurationError = AddMisfitChecks(
            misfitPath,
            specificationValidation.Inventory,
            checkerRegistry,
            checks) || hasConfigurationError;

        GateRunResult result = new GateRunResult(checks, hasConfigurationError);
        WriteArtifacts(
            outputDirectory,
            projectRoot,
            specificationValidation.Inventory,
            result);
        return result;
    }

    private GateCheckResult CreateBlockingCheck(string checkId, List<GateIssue> issues)
    {
        return new GateCheckResult(
            checkId,
            VerificationMode.Blocking,
            issues.Count == 0 ? VerificationStatus.Pass : VerificationStatus.Fail,
            new List<string>(),
            new List<string>(),
            new List<string>(),
            issues);
    }

    private bool AddMisfitChecks(
        string misfitPath,
        SpecificationInventory inventory,
        GateCheckerRegistry checkerRegistry,
        List<GateCheckResult> checks)
    {
        List<GateIssue> configurationIssues = new List<GateIssue>();
        List<GateCheckResult> auditChecks = new List<GateCheckResult>();
        string fullMisfitPath;
        try
        {
            fullMisfitPath = Path.GetFullPath(misfitPath);
        }
        catch (Exception exception)
        {
            configurationIssues.Add(CreateMisfitIssue(
                "V3-MISFIT-CONFIG-PATH",
                misfitPath,
                null,
                $"misfit 路徑無效：{exception.Message}",
                "error"));
            AddMisfitResults(checks, configurationIssues, auditChecks);
            return true;
        }

        if (!File.Exists(fullMisfitPath))
        {
            configurationIssues.Add(CreateMisfitIssue(
                "V3-MISFIT-CONFIG-PATH",
                fullMisfitPath,
                null,
                "找不到 misfit.yaml。",
                "error"));
            AddMisfitResults(checks, configurationIssues, auditChecks);
            return true;
        }

        YamlMappingNode root;
        try
        {
            using StreamReader reader = File.OpenText(fullMisfitPath);
            YamlStream yaml = new YamlStream();
            yaml.Load(reader);
            if (yaml.Documents.Count != 1 || yaml.Documents[0].RootNode is not YamlMappingNode rootMapping)
            {
                configurationIssues.Add(CreateMisfitIssue(
                    "V3-MISFIT-CONFIG-YAML",
                    fullMisfitPath,
                    null,
                    "misfit.yaml 必須包含一份 mapping document。",
                    "error"));
                AddMisfitResults(checks, configurationIssues, auditChecks);
                return true;
            }

            root = rootMapping;
        }
        catch (Exception exception)
        {
            configurationIssues.Add(CreateMisfitIssue(
                "V3-MISFIT-CONFIG-YAML",
                fullMisfitPath,
                null,
                $"讀取 misfit.yaml 失敗：{exception.Message}",
                "error"));
            AddMisfitResults(checks, configurationIssues, auditChecks);
            return true;
        }

        ValidateMisfitRoot(root, fullMisfitPath, configurationIssues);
        HashSet<string> checkIds = new HashSet<string>(StringComparer.Ordinal);
        ReadCustomChecks(
            root,
            fullMisfitPath,
            checkIds,
            inventory,
            checkerRegistry,
            configurationIssues,
            auditChecks);
        ReadManualReviews(root, fullMisfitPath, checkIds, configurationIssues, auditChecks);
        ReadResidualMisfits(root, fullMisfitPath, configurationIssues, auditChecks);
        AddMisfitResults(checks, configurationIssues, auditChecks);
        return configurationIssues.Count > 0;
    }

    private void AddMisfitResults(
        List<GateCheckResult> checks,
        List<GateIssue> configurationIssues,
        List<GateCheckResult> auditChecks)
    {
        checks.Add(CreateBlockingCheck("misfit-configuration", configurationIssues));
        checks.AddRange(auditChecks);
    }

    private void ValidateMisfitRoot(
        YamlMappingNode root,
        string misfitPath,
        List<GateIssue> configurationIssues)
    {
        if (GetScalar(root, "schema_version") != "3")
        {
            configurationIssues.Add(CreateMisfitIssue(
                "V3-MISFIT-SCHEMA",
                misfitPath,
                GetLine(root),
                "misfit.yaml 必須使用 schema_version: 3。",
                "error"));
        }

        HashSet<string> allowedFields = new HashSet<string>(StringComparer.Ordinal)
        {
            "schema_version",
            "custom_checks",
            "manual_reviews",
            "residual_misfits"
        };
        foreach (KeyValuePair<YamlNode, YamlNode> child in root.Children)
        {
            if (child.Key is YamlScalarNode key
                && key.Value != null
                && !allowedFields.Contains(key.Value))
            {
                configurationIssues.Add(CreateMisfitIssue(
                    "V3-MISFIT-UNKNOWN-FIELD",
                    misfitPath,
                    GetLine(child.Key),
                    $"misfit.yaml 不允許欄位：{key.Value}",
                    "error"));
            }
        }

        foreach (string requiredSequence in new[] { "custom_checks", "manual_reviews", "residual_misfits" })
        {
            if (GetNode(root, requiredSequence) is not YamlSequenceNode)
            {
                configurationIssues.Add(CreateMisfitIssue(
                    "V3-MISFIT-SCHEMA",
                    misfitPath,
                    GetLine(root),
                    $"{requiredSequence} 必須是 sequence。",
                    "error"));
            }
        }
    }

    private void ReadCustomChecks(
        YamlMappingNode root,
        string misfitPath,
        HashSet<string> checkIds,
        SpecificationInventory inventory,
        GateCheckerRegistry checkerRegistry,
        List<GateIssue> configurationIssues,
        List<GateCheckResult> auditChecks)
    {
        if (GetNode(root, "custom_checks") is not YamlSequenceNode customChecks)
        {
            return;
        }

        foreach (YamlNode customCheckNode in customChecks.Children)
        {
            if (customCheckNode is not YamlMappingNode customCheck)
            {
                configurationIssues.Add(CreateMisfitIssue(
                    "V3-MISFIT-CUSTOM-CHECK",
                    misfitPath,
                    GetLine(customCheckNode),
                    "custom_checks 的每一項都必須是 mapping。",
                    "error"));
                continue;
            }

            string? checkId = GetScalar(customCheck, "check_id");
            string? checker = GetScalar(customCheck, "checker");
            if (!TryAddCheckId(checkId, checkIds, misfitPath, customCheck, configurationIssues)
                || string.IsNullOrWhiteSpace(checker))
            {
                if (string.IsNullOrWhiteSpace(checker))
                {
                    configurationIssues.Add(CreateMisfitIssue(
                        "V3-MISFIT-CUSTOM-CHECK",
                        misfitPath,
                        GetLine(customCheck),
                        "custom check 必須宣告 checker。",
                        "error"));
                }

                continue;
            }

            List<string> ruleRefs = ReadScalarList(customCheck, "rule_refs");
            ruleRefs.AddRange(ReadScalarList(customCheck, "contract_refs"));
            List<string> evidence = ReadScalarList(customCheck, "scope")
                .Select(scope => $"scope:{scope}")
                .ToList();
            evidence.Insert(0, $"checker:{checker}");
            if (checkerRegistry.TryRun(checker, inventory, out GateCheckResult? checkerResult)
                && checkerResult is not null)
            {
                evidence.AddRange(checkerResult.Evidence);
                auditChecks.Add(new GateCheckResult(
                    checkId!,
                    VerificationMode.Audit,
                    checkerResult.Status,
                    ruleRefs,
                    checkerResult.ScenarioRefs,
                    evidence.Distinct(StringComparer.Ordinal).ToList(),
                    checkerResult.Issues));
                continue;
            }

            GateIssue issue = CreateMisfitIssue(
                "V3-MISFIT-CUSTOM-CHECK-NOT-RUN",
                misfitPath,
                GetLine(customCheck),
                $"自訂 checker 尚未註冊執行：{checker}",
                "warning");
            auditChecks.Add(new GateCheckResult(
                checkId!,
                VerificationMode.Audit,
                VerificationStatus.NotRun,
                ruleRefs,
                new List<string>(),
                evidence,
                new List<GateIssue> { issue }));
        }
    }

    private GateCheckerRegistry CreateCheckerRegistry(string projectRoot)
    {
        GateCheckerRegistry checkerRegistry = new GateCheckerRegistry();
        checkerRegistry.Register(new CSharpApplicationWorkflowStructureChecker(projectRoot));
        checkerRegistry.Register(new CSharpPresenterWorkflowOwnershipChecker(projectRoot));
        checkerRegistry.Register(new CSharpSnakeMovementTopologyChecker(projectRoot));
        checkerRegistry.Register(new CSharpRenderStabilityChecker(projectRoot));
        return checkerRegistry;
    }

    private void ReadManualReviews(
        YamlMappingNode root,
        string misfitPath,
        HashSet<string> checkIds,
        List<GateIssue> configurationIssues,
        List<GateCheckResult> auditChecks)
    {
        if (GetNode(root, "manual_reviews") is not YamlSequenceNode manualReviews)
        {
            return;
        }

        foreach (YamlNode reviewNode in manualReviews.Children)
        {
            if (reviewNode is not YamlMappingNode review)
            {
                configurationIssues.Add(CreateMisfitIssue(
                    "V3-MISFIT-MANUAL-REVIEW",
                    misfitPath,
                    GetLine(reviewNode),
                    "manual_reviews 的每一項都必須是 mapping。",
                    "error"));
                continue;
            }

            string? reviewId = GetScalar(review, "review_id");
            if (!TryAddCheckId(reviewId, checkIds, misfitPath, review, configurationIssues))
            {
                continue;
            }

            VerificationStatus? status = ParseStatus(GetScalar(review, "status"));
            if (status == null)
            {
                configurationIssues.Add(CreateMisfitIssue(
                    "V3-MISFIT-MANUAL-REVIEW",
                    misfitPath,
                    GetLine(review),
                    $"manual review status 無效：{GetScalar(review, "status") ?? "<missing>"}",
                    "error"));
                continue;
            }

            List<string> evidence = ReadScalarList(review, "evidence");
            string? reviewer = GetScalar(review, "reviewer");
            List<GateIssue> reviewIssues = new List<GateIssue>();
            VerificationStatus effectiveStatus = status.Value;
            if (effectiveStatus == VerificationStatus.Pass
                && (string.IsNullOrWhiteSpace(reviewer)
                    || string.Equals(reviewer, "unassigned", StringComparison.OrdinalIgnoreCase)
                    || evidence.Count == 0))
            {
                effectiveStatus = VerificationStatus.Fail;
                reviewIssues.Add(CreateMisfitIssue(
                    "V3-MISFIT-MANUAL-EVIDENCE",
                    misfitPath,
                    GetLine(review),
                    "manual review 不得在 reviewer 未指派或 evidence 為空時標記為 pass。",
                    "warning"));
            }

            if (!string.IsNullOrWhiteSpace(reviewer))
            {
                evidence.Insert(0, $"reviewer:{reviewer}");
            }

            auditChecks.Add(new GateCheckResult(
                reviewId!,
                VerificationMode.Audit,
                effectiveStatus,
                ReadScalarList(review, "contract_refs"),
                new List<string>(),
                evidence,
                reviewIssues));
        }
    }

    private void ReadResidualMisfits(
        YamlMappingNode root,
        string misfitPath,
        List<GateIssue> configurationIssues,
        List<GateCheckResult> auditChecks)
    {
        if (GetNode(root, "residual_misfits") is not YamlSequenceNode residualMisfits)
        {
            return;
        }

        HashSet<string> misfitIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (YamlNode misfitNode in residualMisfits.Children)
        {
            if (misfitNode is not YamlMappingNode misfit)
            {
                configurationIssues.Add(CreateMisfitIssue(
                    "V3-MISFIT-RESIDUAL",
                    misfitPath,
                    GetLine(misfitNode),
                    "residual_misfits 的每一項都必須是 mapping。",
                    "error"));
                continue;
            }

            string? misfitId = GetScalar(misfit, "misfit_id");
            if (string.IsNullOrWhiteSpace(misfitId) || !misfitIds.Add(misfitId))
            {
                configurationIssues.Add(CreateMisfitIssue(
                    "V3-MISFIT-RESIDUAL",
                    misfitPath,
                    GetLine(misfit),
                    $"residual misfit ID 缺少或重複：{misfitId ?? "<missing>"}",
                    "error"));
                continue;
            }

            string? status = GetScalar(misfit, "status");
            VerificationStatus verificationStatus;
            List<GateIssue> misfitIssues = new List<GateIssue>();
            if (status == "open")
            {
                verificationStatus = VerificationStatus.Fail;
                misfitIssues.Add(CreateMisfitIssue(
                    misfitId,
                    misfitPath,
                    GetLine(misfit),
                    GetScalar(misfit, "title") ?? "存在尚未處理的 residual misfit。",
                    "warning"));
            }
            else if (status == "resolved")
            {
                verificationStatus = VerificationStatus.Pass;
            }
            else
            {
                verificationStatus = VerificationStatus.NotRun;
                configurationIssues.Add(CreateMisfitIssue(
                    "V3-MISFIT-RESIDUAL",
                    misfitPath,
                    GetLine(misfit),
                    $"residual misfit status 無效：{status ?? "<missing>"}",
                    "error"));
            }

            auditChecks.Add(new GateCheckResult(
                $"residual:{misfitId}",
                VerificationMode.Audit,
                verificationStatus,
                ReadScalarList(misfit, "contract_refs"),
                new List<string>(),
                ReadScalarList(misfit, "affected_paths"),
                misfitIssues));
        }
    }

    private bool TryAddCheckId(
        string? checkId,
        HashSet<string> checkIds,
        string misfitPath,
        YamlMappingNode node,
        List<GateIssue> configurationIssues)
    {
        if (string.IsNullOrWhiteSpace(checkId) || !checkIds.Add(checkId))
        {
            configurationIssues.Add(CreateMisfitIssue(
                "V3-MISFIT-CHECK-ID",
                misfitPath,
                GetLine(node),
                $"check ID 缺少或重複：{checkId ?? "<missing>"}",
                "error"));
            return false;
        }

        return true;
    }

    private VerificationStatus? ParseStatus(string? status)
    {
        return status switch
        {
            "pass" => VerificationStatus.Pass,
            "fail" => VerificationStatus.Fail,
            "pending" => VerificationStatus.Pending,
            "not_run" => VerificationStatus.NotRun,
            _ => null
        };
    }

    private List<string> ReadScalarList(YamlMappingNode mapping, string fieldName)
    {
        if (GetNode(mapping, fieldName) is not YamlSequenceNode sequence)
        {
            return new List<string>();
        }

        return sequence.Children
            .OfType<YamlScalarNode>()
            .Select(item => item.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
    }

    private void WriteArtifacts(
        string outputDirectory,
        string projectRoot,
        SpecificationInventory inventory,
        GateRunResult result)
    {
        try
        {
            string fullOutputDirectory = Path.GetFullPath(outputDirectory);
            Directory.CreateDirectory(fullOutputDirectory);
            File.WriteAllText(
                Path.Combine(fullOutputDirectory, "verification-manifest.generated.yaml"),
                CreateManifest(projectRoot, inventory, result));
            File.WriteAllText(
                Path.Combine(fullOutputDirectory, "verification-report.json"),
                CreateJsonReport(result));
            File.WriteAllText(
                Path.Combine(fullOutputDirectory, "verification-report.md"),
                CreateMarkdownReport(result));
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"寫入 project verification 產物失敗：{outputDirectory}；{exception.Message}",
                exception);
        }
    }

    private string CreateManifest(
        string projectRoot,
        SpecificationInventory inventory,
        GateRunResult result)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("schema_version: 3");
        builder.AppendLine("generated: true");
        builder.AppendLine("specifications:");
        foreach (SpecificationDocument document in inventory.Documents.OrderBy(
            document => document.Header.Id,
            StringComparer.Ordinal))
        {
            builder.AppendLine($"  - id: \"{EscapeYaml(document.Header.Id)}\"");
            builder.AppendLine($"    kind: {ToYamlSpecificationKind(document.Header.SpecificationKind)}");
            string relativePath = Path.GetRelativePath(projectRoot, document.FilePath).Replace('\\', '/');
            builder.AppendLine($"    path: \"{EscapeYaml(relativePath)}\"");
        }

        builder.AppendLine("checks:");
        foreach (GateCheckResult check in result.Checks)
        {
            builder.AppendLine($"  - check_id: \"{EscapeYaml(check.CheckId)}\"");
            builder.AppendLine($"    mode: {check.Mode.ToString().ToLowerInvariant()}");
            builder.AppendLine($"    status: {ToYamlStatus(check.Status)}");
            AppendYamlList(builder, "    ", "rule_refs", check.RuleRefs);
            AppendYamlList(builder, "    ", "scenario_refs", check.ScenarioRefs);
            AppendYamlList(builder, "    ", "evidence", check.Evidence);
        }

        return builder.ToString();
    }

    private void AppendYamlList(
        StringBuilder builder,
        string indentation,
        string fieldName,
        List<string> values)
    {
        if (values.Count == 0)
        {
            builder.AppendLine($"{indentation}{fieldName}: []");
            return;
        }

        builder.AppendLine($"{indentation}{fieldName}:");
        foreach (string value in values)
        {
            builder.AppendLine($"{indentation}  - \"{EscapeYaml(value)}\"");
        }
    }

    private string CreateMarkdownReport(GateRunResult result)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Pattern Network Verification Report");
        builder.AppendLine();
        builder.AppendLine($"Overall: **{(result.IsSuccessful ? "PASS" : "FAIL")}**");
        builder.AppendLine();
        builder.AppendLine("| Check | Mode | Status |");
        builder.AppendLine("| --- | --- | --- |");
        foreach (GateCheckResult check in result.Checks)
        {
            builder.AppendLine(
                $"| {EscapeMarkdown(check.CheckId)} | {check.Mode.ToString().ToUpperInvariant()} | {ToYamlStatus(check.Status).ToUpperInvariant()} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Issues");
        builder.AppendLine();
        if (result.Issues.Count == 0)
        {
            builder.AppendLine("- None");
        }
        else
        {
            foreach (GateIssue issue in result.Issues)
            {
                string location = issue.Line.HasValue
                    ? $"{issue.File}:{issue.Line.Value}"
                    : issue.File;
                builder.AppendLine(
                    $"- [{issue.Severity.ToUpperInvariant()}] `{EscapeMarkdown(issue.RuleId)}` {EscapeMarkdown(location)} — {EscapeMarkdown(issue.Message)}");
            }
        }

        return builder.ToString();
    }

    private string CreateJsonReport(GateRunResult result)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        object report = new
        {
            result.IsSuccessful,
            result.HasConfigurationError,
            Checks = result.Checks.Select(check => new
            {
                check.CheckId,
                Mode = check.Mode.ToString().ToLowerInvariant(),
                Status = ToYamlStatus(check.Status),
                check.RuleRefs,
                check.ScenarioRefs,
                check.Evidence
            }).ToList(),
            Issues = result.Issues.Select(issue => new
            {
                issue.RuleId,
                issue.Node,
                issue.File,
                issue.Line,
                issue.Message,
                issue.Severity
            }).ToList()
        };
        return JsonSerializer.Serialize(report, options);
    }

    private string ToYamlStatus(VerificationStatus status)
    {
        return status switch
        {
            VerificationStatus.Pass => "pass",
            VerificationStatus.Fail => "fail",
            VerificationStatus.Pending => "pending",
            VerificationStatus.NotRun => "not_run",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "不支援的 verification status。")
        };
    }

    private string ToYamlSpecificationKind(SpecificationKind specificationKind)
    {
        return specificationKind switch
        {
            SpecificationKind.ProblemFrame => "problem_frame",
            SpecificationKind.AggregateContract => "aggregate_contract",
            SpecificationKind.WorkflowContract => "workflow_contract",
            SpecificationKind.TechnicalContract => "technical_contract",
            SpecificationKind.AcceptanceContract => "acceptance_contract",
            SpecificationKind.JourneyContract => "journey_contract",
            SpecificationKind.PresentationState => "presentation_state",
            SpecificationKind.PresentationWorkflow => "presentation_workflow",
            SpecificationKind.SpecificationCatalog => "spec_catalog",
            SpecificationKind.MigrationMap => "migration_map",
            _ => throw new ArgumentOutOfRangeException(
                nameof(specificationKind),
                specificationKind,
                "不支援的 specification kind。")
        };
    }

    private string EscapeYaml(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private string EscapeMarkdown(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
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

    private GateIssue CreateMisfitIssue(
        string ruleId,
        string file,
        int? line,
        string message,
        string severity)
    {
        return new GateIssue(ruleId, "Misfit", file, line, message, severity);
    }
}
