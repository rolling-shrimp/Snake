namespace PatternNetwork.Gates;

public class CSharpPresenterWorkflowOwnershipChecker : IGateChecker
{
    private string _projectRoot;

    public CSharpPresenterWorkflowOwnershipChecker(string projectRoot)
    {
        _projectRoot = Path.GetFullPath(projectRoot);
    }

    public string Id => "csharp_presenter_workflow_ownership";

    public GateCheckResult Check(SpecificationInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        string presenterPath = Path.Combine(_projectRoot, "Snake.WinForms", "GamePresenter.cs");
        List<GateIssue> issues = new List<GateIssue>();
        List<string> evidence = new List<string> { $"presenter:{presenterPath}" };
        if (!File.Exists(presenterPath))
        {
            issues.Add(CreateIssue(presenterPath, null, "找不到 GamePresenter.cs。"));
            return CreateResult(issues, evidence);
        }

        string source = File.ReadAllText(presenterPath);
        List<string> forbiddenTokens = new List<string>
        {
            "SnakeGameSession",
            ".Execute(",
            "ReadGameplay(",
            "AdvanceSnakeStep",
            "HandleSelfCollision",
            "HandleSpecialFoodContact"
        };
        foreach (string forbiddenToken in forbiddenTokens)
        {
            int index = source.IndexOf(forbiddenToken, StringComparison.Ordinal);
            if (index >= 0)
            {
                issues.Add(CreateIssue(
                    presenterPath,
                    GetLine(source, index),
                    $"Presenter 不得擁有或呼叫葉節點業務步驟：{forbiddenToken}。"));
            }
        }

        List<string> workflowMethods = new List<string>
        {
            "GetCurrentScreen",
            "StartCurrentLevel",
            "SetSnakeDirection",
            "ProcessGameTick",
            "RestartGame"
        };
        foreach (string workflowMethod in workflowMethods)
        {
            string call = $"_applicationService.{workflowMethod}(";
            int callCount = CountOccurrences(source, call);
            if (callCount != 1)
            {
                issues.Add(CreateIssue(
                    presenterPath,
                    null,
                    $"Presenter 對 {workflowMethod} 應恰有一個 UI 邊界呼叫，實際為 {callCount}。"));
            }
        }

        int directiveIndex = source.IndexOf(
            "_view.ApplyRuntimeDirective(runtimeDirective)",
            StringComparison.Ordinal);
        int renderIndex = source.IndexOf("_view.RenderGameScreen(screen)", StringComparison.Ordinal);
        if (directiveIndex < 0 || renderIndex <= directiveIndex)
        {
            issues.Add(CreateIssue(
                presenterPath,
                null,
                "Presenter 必須先套用 RuntimeDirective，再提交一次完整 GameScreenOutput。"));
        }

        evidence.Add($"workflow-call-count:{workflowMethods.Count}");
        return CreateResult(issues, evidence);
    }

    private int CountOccurrences(string source, string value)
    {
        int count = 0;
        int searchIndex = 0;
        while ((searchIndex = source.IndexOf(value, searchIndex, StringComparison.Ordinal)) >= 0)
        {
            count++;
            searchIndex += value.Length;
        }

        return count;
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
            "V3-CODE-PRESENTER-WORKFLOW-OWNERSHIP",
            "GamePresenter",
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
