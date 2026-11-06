using System.Text.RegularExpressions;

namespace PatternNetwork.Gates;

public class CSharpApplicationWorkflowStructureChecker : IGateChecker
{
    private string _projectRoot;

    public CSharpApplicationWorkflowStructureChecker(string projectRoot)
    {
        _projectRoot = Path.GetFullPath(projectRoot);
    }

    public string Id => "csharp_application_workflow_structure";

    public GateCheckResult Check(SpecificationInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        List<GateIssue> issues = new List<GateIssue>();
        string useCasesPath = Path.Combine(_projectRoot, "Snake.UseCases");
        if (!Directory.Exists(useCasesPath))
        {
            issues.Add(CreateIssue(useCasesPath, null, "找不到 Snake.UseCases 專案目錄。"));
            return CreateResult(issues, new List<string>());
        }

        foreach (string file in Directory.GetFiles(useCasesPath, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            Match leafUseCase = Regex.Match(source, @"public\s+class\s+\w*UseCase\b");
            if (leafUseCase.Success)
            {
                issues.Add(CreateIssue(
                    file,
                    GetLine(source, leafUseCase.Index),
                    "不得保留公開葉節點 ...UseCase class。"));
            }

            int executeIndex = source.IndexOf(" Execute(", StringComparison.Ordinal);
            if (executeIndex >= 0)
            {
                issues.Add(CreateIssue(
                    file,
                    GetLine(source, executeIndex),
                    "Application Workflow 不得以公開 Execute 葉節點取代完整流程。"));
            }
        }

        string servicePath = Path.Combine(useCasesPath, "SnakeGameApplicationService.cs");
        List<string> requiredMethods = new List<string>
        {
            "StartCurrentLevel",
            "SetSnakeDirection",
            "ProcessGameTick",
            "RestartGame",
            "GetCurrentScreen"
        };
        if (!File.Exists(servicePath))
        {
            issues.Add(CreateIssue(servicePath, null, "缺少 SnakeGameApplicationService。"));
        }
        else
        {
            string serviceSource = File.ReadAllText(servicePath);
            foreach (string method in requiredMethods)
            {
                if (!Regex.IsMatch(serviceSource, $@"public\s+\w+Result\s+{method}\s*\("))
                {
                    issues.Add(CreateIssue(
                        servicePath,
                        null,
                        $"缺少公開完整 Workflow method：{method}。"));
                }
            }
        }

        return CreateResult(
            issues,
            new List<string>
            {
                $"application-service:{servicePath}",
                $"required-workflows:{string.Join(",", requiredMethods)}"
            });
    }

    private GateCheckResult CreateResult(List<GateIssue> issues, List<string> evidence)
    {
        return new GateCheckResult(
            Id,
            VerificationMode.Audit,
            issues.Count == 0 ? VerificationStatus.Pass : VerificationStatus.Fail,
            new List<string>(),
            new List<string>(),
            evidence,
            issues);
    }

    private GateIssue CreateIssue(string file, int? line, string message)
    {
        return new GateIssue(
            "V3-CODE-APPLICATION-WORKFLOW-STRUCTURE",
            "Snake.UseCases",
            file,
            line,
            message,
            "warning");
    }

    private int GetLine(string source, int index)
    {
        return source.Take(index).Count(character => character == '\n') + 1;
    }
}
