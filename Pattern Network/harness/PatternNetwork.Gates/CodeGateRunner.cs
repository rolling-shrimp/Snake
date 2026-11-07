using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace PatternNetwork.Gates;

public class CodeGateRunner
{
    public GateRunResult CheckCode(string networkPath, string solutionPath, string? nodeFilter)
    {
        List<GateIssue> issues = new List<GateIssue>();
        bool hasConfigurationError = false;
        PatternNetworkValidator validator = new PatternNetworkValidator();
        PatternNetworkValidationResult validationResult = validator.Validate(networkPath);
        if (!validationResult.IsValid)
        {
            return new GateRunResult(validationResult.Issues, true);
        }

        string fullSolutionPath = Path.GetFullPath(solutionPath);
        if (!File.Exists(fullSolutionPath))
        {
            issues.Add(new GateIssue("V3-PN-SOLUTION-NOT-FOUND", "network", fullSolutionPath, null, $"找不到 solution：{fullSolutionPath}", "error"));
            return new GateRunResult(issues, true);
        }

        List<PatternNodeDefinition> selectedNodes = validationResult.Definition.PatternNodes;
        if (!string.IsNullOrWhiteSpace(nodeFilter))
        {
            selectedNodes = selectedNodes.Where(node => string.Equals(node.Id, nodeFilter, StringComparison.Ordinal)).ToList();
            if (selectedNodes.Count == 0)
            {
                issues.Add(new GateIssue("V3-PN-NODE-NOT-FOUND", nodeFilter, fullSolutionPath, null, $"Pattern Network 不存在節點：{nodeFilter}", "error"));
                return new GateRunResult(issues, true);
            }
        }

        string solutionDirectory = Path.GetDirectoryName(fullSolutionPath)!;
        string solutionText;
        try
        {
            solutionText = File.ReadAllText(fullSolutionPath);
        }
        catch (Exception exception)
        {
            issues.Add(new GateIssue("V3-PN-SOLUTION-READ", "network", fullSolutionPath, null, $"讀取 solution 失敗：{exception.Message}", "error"));
            return new GateRunResult(issues, true);
        }

        ProjectNamePolicy projectNamePolicy = new ProjectNamePolicy();
        foreach (PatternNodeDefinition patternNode in selectedNodes)
        {
            string projectName;
            try
            {
                projectName = projectNamePolicy.GetProjectName(fullSolutionPath, patternNode.Name);
            }
            catch (ArgumentException exception)
            {
                issues.Add(new GateIssue("V3-PN-NAME-001", patternNode.Id, fullSolutionPath, null, exception.Message, "error"));
                hasConfigurationError = true;
                continue;
            }

            string projectDirectory = Path.Combine(solutionDirectory, patternNode.SolutionFolder, projectName);
            string projectPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
            if (!File.Exists(projectPath))
            {
                issues.Add(new GateIssue("V3-PN-TARGET-NOT-FOUND", patternNode.Id, projectPath, null, $"依命名公式推導的產品專案不存在：{projectName}", "error"));
                hasConfigurationError = true;
                continue;
            }

            ValidateSolutionEntry(
                solutionText,
                fullSolutionPath,
                projectName,
                patternNode.SolutionFolder,
                issues,
                patternNode);
            ValidateProject(projectPath, projectName, fullSolutionPath, patternNode, issues, ref hasConfigurationError);
            CSharpGateInspector inspector = new CSharpGateInspector();
            issues.AddRange(inspector.Inspect(patternNode, projectDirectory));
        }

        PatternNodeDefinition? selectedApplicationNode = selectedNodes.FirstOrDefault(node => string.Equals(node.Id, "application", StringComparison.Ordinal));
        PatternNodeDefinition? domainNode = validationResult.Definition.PatternNodes.FirstOrDefault(node => string.Equals(node.Id, "domain", StringComparison.Ordinal));
        if (selectedApplicationNode != null && domainNode != null)
        {
            string domainProjectName = projectNamePolicy.GetProjectName(fullSolutionPath, domainNode.Name);
            string applicationProjectName = projectNamePolicy.GetProjectName(fullSolutionPath, selectedApplicationNode.Name);
            string domainProjectDirectory = Path.Combine(solutionDirectory, domainNode.SolutionFolder, domainProjectName);
            string applicationProjectDirectory = Path.Combine(solutionDirectory, selectedApplicationNode.SolutionFolder, applicationProjectName);
            if (Directory.Exists(domainProjectDirectory) && Directory.Exists(applicationProjectDirectory))
            {
                SemanticGateInspector semanticInspector = new SemanticGateInspector();
                issues.AddRange(semanticInspector.InspectApplicationContracts(
                    selectedApplicationNode,
                    fullSolutionPath,
                    domainProjectDirectory,
                    applicationProjectDirectory));
            }
        }

        if (string.IsNullOrWhiteSpace(nodeFilter))
        {
            ValidateVerificationProject(
                validationResult.Definition.VerificationProject,
                projectNamePolicy,
                solutionText,
                fullSolutionPath,
                issues,
                ref hasConfigurationError);
        }

        return CreateRunResult(SortIssues(issues), hasConfigurationError);
    }

    private void ValidateSolutionEntry(
        string solutionText,
        string solutionPath,
        string projectName,
        string solutionFolder,
        List<GateIssue> issues,
        PatternNodeDefinition patternNode)
    {
        string expectedEntry = $"= \"{projectName}\", \"{solutionFolder}\\{projectName}\\{projectName}.csproj\"";
        if (!solutionText.Contains(expectedEntry, StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("V3-PN-NAME-001", patternNode.Id, solutionPath, null, $"Solution 必須以推導名稱與路徑包含專案 {projectName}。", "error"));
            return;
        }

        ValidateSolutionFolderNesting(
            solutionText,
            solutionPath,
            projectName,
            solutionFolder,
            patternNode.Id,
            issues);
    }

    private void ValidateProject(
        string projectPath,
        string projectName,
        string solutionPath,
        PatternNodeDefinition patternNode,
        List<GateIssue> issues,
        ref bool hasConfigurationError)
    {
        try
        {
            XDocument projectDocument = XDocument.Load(projectPath);
            ValidateProjectIdentity(projectDocument, projectPath, projectName, patternNode, issues);
            ValidateProjectReferences(projectDocument, projectPath, solutionPath, patternNode, issues);
        }
        catch (Exception exception)
        {
            issues.Add(new GateIssue("V3-PN-PROJECT-READ", patternNode.Id, projectPath, null, $"讀取專案檔失敗：{exception.Message}", "error"));
            hasConfigurationError = true;
        }
    }

    private void ValidateProjectIdentity(
        XDocument projectDocument,
        string projectPath,
        string projectName,
        PatternNodeDefinition patternNode,
        List<GateIssue> issues)
    {
        GateCheckDefinition identityCheck = GetRequiredCheck(patternNode, "project_identity");
        string expectedTargetFramework = identityCheck.ScalarParameters["target_framework"];
        bool expectedUseWindowsForms = bool.Parse(identityCheck.ScalarParameters["use_windows_forms"]);
        string? targetFramework = projectDocument.Descendants("TargetFramework").Select(element => element.Value.Trim()).FirstOrDefault();
        if (!string.Equals(targetFramework, expectedTargetFramework, StringComparison.Ordinal))
        {
            issues.Add(new GateIssue(identityCheck.Id, patternNode.Id, projectPath, null, $"TargetFramework 必須是 {expectedTargetFramework}，實際為 {targetFramework ?? "<missing>"}。", "error"));
        }

        string? useWindowsFormsText = projectDocument.Descendants("UseWindowsForms").Select(element => element.Value.Trim()).FirstOrDefault();
        bool actualUseWindowsForms = bool.TryParse(useWindowsFormsText, out bool parsedUseWindowsForms) && parsedUseWindowsForms;
        if (actualUseWindowsForms != expectedUseWindowsForms)
        {
            issues.Add(new GateIssue(identityCheck.Id, patternNode.Id, projectPath, null, $"UseWindowsForms 必須是 {expectedUseWindowsForms.ToString().ToLowerInvariant()}。", "error"));
        }

        string? assemblyName = projectDocument.Descendants("AssemblyName").Select(element => element.Value.Trim()).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(assemblyName) && !string.Equals(assemblyName, projectName, StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("V3-PN-NAME-001", patternNode.Id, projectPath, null, $"AssemblyName 必須等於推導名稱 {projectName}。", "error"));
        }

        string? rootNamespace = projectDocument.Descendants("RootNamespace").Select(element => element.Value.Trim()).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(rootNamespace) && !string.Equals(rootNamespace, projectName, StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("V3-PN-NAME-001", patternNode.Id, projectPath, null, $"RootNamespace 必須等於推導名稱 {projectName}。", "error"));
        }

        Dictionary<string, string> actualPackages = projectDocument.Descendants("PackageReference")
            .Where(element => element.Attribute("Include") != null)
            .ToDictionary(
                element => element.Attribute("Include")!.Value,
                element => element.Attribute("Version")?.Value
                    ?? element.Elements("Version").Select(version => version.Value).FirstOrDefault()
                    ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string> requiredPackage in patternNode.RequiredPackageReferences)
        {
            if (!actualPackages.TryGetValue(requiredPackage.Key, out string? actualVersion)
                || !string.Equals(actualVersion, requiredPackage.Value, StringComparison.Ordinal))
            {
                issues.Add(new GateIssue(
                    "V3-PN-ARCH-001",
                    patternNode.Id,
                    projectPath,
                    null,
                    $"缺少必要 PackageReference：{requiredPackage.Key}@{requiredPackage.Value}",
                    "error"));
            }
        }
    }

    private void ValidateProjectReferences(
        XDocument projectDocument,
        string projectPath,
        string solutionPath,
        PatternNodeDefinition patternNode,
        List<GateIssue> issues)
    {
        ProjectNamePolicy projectNamePolicy = new ProjectNamePolicy();
        GateCheckDefinition referenceCheck = GetRequiredCheck(patternNode, "project_references");
        List<string> requiredPatternNodes = referenceCheck.ListParameters["required_pattern_nodes"];
        HashSet<string> expectedReferences = requiredPatternNodes
            .Select(reference => projectNamePolicy.GetProjectName(solutionPath, reference))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> actualReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (XElement projectReference in projectDocument.Descendants("ProjectReference"))
        {
            string? includePath = projectReference.Attribute("Include")?.Value;
            if (!string.IsNullOrWhiteSpace(includePath))
            {
                actualReferences.Add(Path.GetFileNameWithoutExtension(includePath.Replace('\\', Path.DirectorySeparatorChar)));
            }
        }

        foreach (string missingReference in expectedReferences.Except(actualReferences, StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(new GateIssue("V3-PN-ARCH-001", patternNode.Id, projectPath, null, $"缺少必要 ProjectReference：{missingReference}", "error"));
        }

        foreach (string forbiddenReference in actualReferences.Except(expectedReferences, StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(new GateIssue("V3-PN-ARCH-001", patternNode.Id, projectPath, null, $"不允許的 ProjectReference：{forbiddenReference}", "error"));
        }
    }

    private GateCheckDefinition GetRequiredCheck(PatternNodeDefinition patternNode, string checker)
    {
        GateCheckDefinition? check = patternNode.Checks.FirstOrDefault(candidate => string.Equals(candidate.Checker, checker, StringComparison.Ordinal));
        if (check == null)
        {
            throw new InvalidDataException($"節點 {patternNode.Id} 缺少必要 checker：{checker}");
        }

        return check;
    }

    private void ValidateVerificationProject(
        VerificationProjectDefinition verification,
        ProjectNamePolicy projectNamePolicy,
        string solutionText,
        string solutionPath,
        List<GateIssue> issues,
        ref bool hasConfigurationError)
    {
        string projectName = projectNamePolicy.GetProjectName(solutionPath, verification.Name);
        string solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        string projectDirectory = Path.Combine(solutionDirectory, verification.SolutionFolder, projectName);
        string projectPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
        PatternNodeDefinition verificationNode = new PatternNodeDefinition
        {
            Id = "verification",
            Name = verification.Name,
            SolutionFolder = verification.SolutionFolder
        };
        if (!File.Exists(projectPath))
        {
            issues.Add(new GateIssue("V3-PN-TARGET-NOT-FOUND", "verification", projectPath, null, $"找不到測試專案：{projectName}", "error"));
            hasConfigurationError = true;
            return;
        }

        ValidateSolutionEntry(
            solutionText,
            solutionPath,
            projectName,
            verification.SolutionFolder,
            issues,
            verificationNode);
        try
        {
            XDocument projectDocument = XDocument.Load(projectPath);
            string? targetFramework = projectDocument.Descendants("TargetFramework").Select(element => element.Value.Trim()).FirstOrDefault();
            bool useWindowsForms = bool.TryParse(
                projectDocument.Descendants("UseWindowsForms").Select(element => element.Value.Trim()).FirstOrDefault(),
                out bool parsedUseWindowsForms) && parsedUseWindowsForms;
            if (!string.Equals(targetFramework, verification.TargetFramework, StringComparison.Ordinal)
                || useWindowsForms != verification.UseWindowsForms)
            {
                issues.Add(new GateIssue("V3-PN-ARCH-002", "verification", projectPath, null, "測試專案的 TargetFramework 或 UseWindowsForms 不符合 Pattern Network。", "error"));
            }

            HashSet<string> actualReferences = projectDocument.Descendants("ProjectReference")
                .Select(reference => reference.Attribute("Include")?.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Path.GetFileNameWithoutExtension(value!.Replace('\\', Path.DirectorySeparatorChar)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            HashSet<string> expectedReferences = verification.RequiredProjectReferences
                .Select(reference => projectNamePolicy.GetProjectName(solutionPath, reference))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!actualReferences.SetEquals(expectedReferences))
            {
                issues.Add(new GateIssue("V3-PN-ARCH-002", "verification", projectPath, null, "測試專案必須且只能參考 Domain、Application、Infrastructure 與 WinForms。", "error"));
            }
        }
        catch (Exception exception)
        {
            issues.Add(new GateIssue("V3-PN-PROJECT-READ", "verification", projectPath, null, $"讀取測試專案失敗：{exception.Message}", "error"));
            hasConfigurationError = true;
        }
    }

    private void ValidateSolutionFolderNesting(
        string solutionText,
        string solutionPath,
        string projectName,
        string solutionFolder,
        string node,
        List<GateIssue> issues)
    {
        Match projectMatch = Regex.Match(
            solutionText,
            $"Project\\(\\\"[^\\\"]+\\\"\\) = \\\"{Regex.Escape(projectName)}\\\", \\\"[^\\\"]+\\\", \\\"(?<guid>\\{{[^\\}}]+\\}})\\\"");
        Match folderMatch = Regex.Match(
            solutionText,
            $"Project\\(\\\"\\{{2150E333-8FDC-42A3-9474-1A3956D46DE8\\}}\\\"\\) = \\\"{Regex.Escape(solutionFolder)}\\\", \\\"{Regex.Escape(solutionFolder)}\\\", \\\"(?<guid>\\{{[^\\}}]+\\}})\\\"",
            RegexOptions.IgnoreCase);
        if (!projectMatch.Success
            || !folderMatch.Success
            || !Regex.IsMatch(
                solutionText,
                $"{Regex.Escape(projectMatch.Groups["guid"].Value)}\\s*=\\s*{Regex.Escape(folderMatch.Groups["guid"].Value)}",
                RegexOptions.IgnoreCase))
        {
            issues.Add(new GateIssue(
                "V3-PN-ARCH-002",
                node,
                solutionPath,
                null,
                $"專案 {projectName} 必須巢狀於 {solutionFolder} solution folder。",
                "error"));
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

    private GateRunResult CreateRunResult(List<GateIssue> issues, bool hasConfigurationError)
    {
        List<GateIssue> auditIssues = issues
            .Where(issue => string.Equals(issue.Severity, "warning", StringComparison.OrdinalIgnoreCase))
            .ToList();
        List<GateIssue> blockingIssues = issues
            .Where(issue => !string.Equals(issue.Severity, "warning", StringComparison.OrdinalIgnoreCase))
            .ToList();
        List<GateCheckResult> checks = new List<GateCheckResult>
        {
            new GateCheckResult(
                "code-architecture",
                VerificationMode.Blocking,
                blockingIssues.Count == 0 ? VerificationStatus.Pass : VerificationStatus.Fail,
                new List<string>(),
                new List<string>(),
                new List<string>(),
                blockingIssues),
            new GateCheckResult(
                "code-architecture-audit",
                VerificationMode.Audit,
                auditIssues.Count == 0 ? VerificationStatus.Pass : VerificationStatus.Fail,
                new List<string>(),
                new List<string>(),
                new List<string>(),
                auditIssues)
        };
        return new GateRunResult(checks, hasConfigurationError);
    }
}
