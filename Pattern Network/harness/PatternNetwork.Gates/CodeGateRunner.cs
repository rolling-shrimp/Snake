using System.Xml.Linq;

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
            issues.Add(new GateIssue("PN-SOLUTION-NOT-FOUND", "network", fullSolutionPath, null, $"找不到 solution：{fullSolutionPath}", "error"));
            return new GateRunResult(issues, true);
        }

        List<PatternNodeDefinition> selectedNodes = validationResult.Definition.PatternNodes;
        if (!string.IsNullOrWhiteSpace(nodeFilter))
        {
            selectedNodes = selectedNodes.Where(node => string.Equals(node.Id, nodeFilter, StringComparison.Ordinal)).ToList();
            if (selectedNodes.Count == 0)
            {
                issues.Add(new GateIssue("PN-NODE-NOT-FOUND", nodeFilter, fullSolutionPath, null, $"Pattern Network 不存在節點：{nodeFilter}", "error"));
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
            issues.Add(new GateIssue("PN-SOLUTION-READ", "network", fullSolutionPath, null, $"讀取 solution 失敗：{exception.Message}", "error"));
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
                issues.Add(new GateIssue("PN-NAME-001", patternNode.Id, fullSolutionPath, null, exception.Message, "error"));
                hasConfigurationError = true;
                continue;
            }

            string projectDirectory = Path.Combine(solutionDirectory, projectName);
            string projectPath = Path.Combine(projectDirectory, $"{projectName}.csproj");
            if (!File.Exists(projectPath))
            {
                issues.Add(new GateIssue("PN-TARGET-NOT-FOUND", patternNode.Id, projectPath, null, $"依命名公式推導的產品專案不存在：{projectName}", "error"));
                hasConfigurationError = true;
                continue;
            }

            ValidateSolutionEntry(solutionText, fullSolutionPath, projectName, issues, patternNode);
            ValidateProject(projectPath, projectName, fullSolutionPath, patternNode, issues, ref hasConfigurationError);
            CSharpGateInspector inspector = new CSharpGateInspector();
            issues.AddRange(inspector.Inspect(patternNode, projectDirectory));
        }

        PatternNodeDefinition? selectedUseCaseNode = selectedNodes.FirstOrDefault(node => string.Equals(node.Id, "use-cases", StringComparison.Ordinal));
        PatternNodeDefinition? entitiesNode = validationResult.Definition.PatternNodes.FirstOrDefault(node => string.Equals(node.Id, "entities", StringComparison.Ordinal));
        if (selectedUseCaseNode != null && entitiesNode != null)
        {
            string entitiesProjectName = projectNamePolicy.GetProjectName(fullSolutionPath, entitiesNode.Name);
            string useCasesProjectName = projectNamePolicy.GetProjectName(fullSolutionPath, selectedUseCaseNode.Name);
            string entitiesProjectDirectory = Path.Combine(solutionDirectory, entitiesProjectName);
            string useCasesProjectDirectory = Path.Combine(solutionDirectory, useCasesProjectName);
            if (Directory.Exists(entitiesProjectDirectory) && Directory.Exists(useCasesProjectDirectory))
            {
                SemanticGateInspector semanticInspector = new SemanticGateInspector();
                issues.AddRange(semanticInspector.InspectUseCaseContracts(selectedUseCaseNode, fullSolutionPath, entitiesProjectDirectory, useCasesProjectDirectory));
            }
        }

        return new GateRunResult(SortIssues(issues), hasConfigurationError);
    }

    private void ValidateSolutionEntry(
        string solutionText,
        string solutionPath,
        string projectName,
        List<GateIssue> issues,
        PatternNodeDefinition patternNode)
    {
        string expectedEntry = $"= \"{projectName}\", \"{projectName}\\{projectName}.csproj\"";
        if (!solutionText.Contains(expectedEntry, StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("PN-NAME-001", patternNode.Id, solutionPath, null, $"Solution 必須以推導名稱與路徑包含專案 {projectName}。", "error"));
        }
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
            issues.Add(new GateIssue("PN-PROJECT-READ", patternNode.Id, projectPath, null, $"讀取專案檔失敗：{exception.Message}", "error"));
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
            issues.Add(new GateIssue("PN-NAME-001", patternNode.Id, projectPath, null, $"AssemblyName 必須等於推導名稱 {projectName}。", "error"));
        }

        string? rootNamespace = projectDocument.Descendants("RootNamespace").Select(element => element.Value.Trim()).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(rootNamespace) && !string.Equals(rootNamespace, projectName, StringComparison.Ordinal))
        {
            issues.Add(new GateIssue("PN-NAME-001", patternNode.Id, projectPath, null, $"RootNamespace 必須等於推導名稱 {projectName}。", "error"));
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
            issues.Add(new GateIssue("PN-ARCH-001", patternNode.Id, projectPath, null, $"缺少必要 ProjectReference：{missingReference}", "error"));
        }

        foreach (string forbiddenReference in actualReferences.Except(expectedReferences, StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(new GateIssue("PN-ARCH-001", patternNode.Id, projectPath, null, $"不允許的 ProjectReference：{forbiddenReference}", "error"));
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
