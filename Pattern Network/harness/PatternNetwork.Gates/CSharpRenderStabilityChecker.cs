namespace PatternNetwork.Gates;

public class CSharpRenderStabilityChecker : IGateChecker
{
    private string _projectRoot;

    public CSharpRenderStabilityChecker(string projectRoot)
    {
        _projectRoot = Path.GetFullPath(projectRoot);
    }

    public string Id => "csharp_render_stability";

    public GateCheckResult Check(SpecificationInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        string formPath = Path.Combine(_projectRoot, "Snake.WinForms", "GameForm.cs");
        string presenterPath = Path.Combine(_projectRoot, "Snake.WinForms", "GamePresenter.cs");
        string testPath = Path.Combine(_projectRoot, "Snake.Test", "GameFormRenderingTests.cs");
        List<GateIssue> issues = new List<GateIssue>();
        if (!File.Exists(formPath))
        {
            issues.Add(CreateIssue(formPath, null, "找不到 GameForm.cs。"));
        }
        else
        {
            string formSource = File.ReadAllText(formPath);
            CheckForbidden(formSource, formPath, "Controls.Clear", issues);
            CheckRequired(formSource, formPath, "RenderGameScreen(GameScreenOutput screen)", issues);
            CheckRequired(formSource, formPath, "_ruleLabels", issues);
            CheckRequired(formSource, formPath, "SuspendLayout()", issues);
            CheckRequired(formSource, formPath, "ResumeLayout(true)", issues);
            string renderBody = GetMethodRegion(
                formSource,
                "public void RenderGameScreen",
                "public void ShowFailure");
            if (renderBody.Contains("_gameTimer", StringComparison.Ordinal)
                || renderBody.Contains("_levelClock", StringComparison.Ordinal))
            {
                issues.Add(CreateIssue(
                    formPath,
                    null,
                    "RenderGameScreen 不得擁有 Timer 或 runtime clock。"));
            }
        }

        if (!File.Exists(presenterPath))
        {
            issues.Add(CreateIssue(presenterPath, null, "找不到 GamePresenter.cs。"));
        }
        else
        {
            string presenterSource = File.ReadAllText(presenterPath);
            int directiveIndex = presenterSource.IndexOf(
                "_view.ApplyRuntimeDirective(runtimeDirective)",
                StringComparison.Ordinal);
            int renderIndex = presenterSource.IndexOf(
                "_view.RenderGameScreen(screen)",
                StringComparison.Ordinal);
            if (directiveIndex < 0 || renderIndex <= directiveIndex)
            {
                issues.Add(CreateIssue(
                    presenterPath,
                    null,
                    "UI 結果必須依序套用 directive 與單一完整 screen。"));
            }
        }

        if (!File.Exists(testPath)
            || !File.ReadAllText(testPath).Contains(
                "RepeatedRender_PreservesStableControlIdentity",
                StringComparison.Ordinal))
        {
            issues.Add(CreateIssue(testPath, null, "缺少重複 render 控制項身分測試。"));
        }

        return new GateCheckResult(
            Id,
            VerificationMode.Audit,
            issues.Count == 0 ? VerificationStatus.Pass : VerificationStatus.Fail,
            new List<string>(),
            new List<string> { "V3-SCN-RENDER-STABLE-CONTROL-IDENTITY" },
            new List<string>
            {
                $"renderer:{formPath}",
                $"runtime-order:{presenterPath}",
                $"identity-test:{testPath}"
            },
            issues);
    }

    private void CheckForbidden(
        string source,
        string file,
        string token,
        List<GateIssue> issues)
    {
        int index = source.IndexOf(token, StringComparison.Ordinal);
        if (index >= 0)
        {
            issues.Add(CreateIssue(
                file,
                GetLine(source, index),
                $"Renderer 不得使用：{token}。"));
        }
    }

    private void CheckRequired(
        string source,
        string file,
        string token,
        List<GateIssue> issues)
    {
        if (!source.Contains(token, StringComparison.Ordinal))
        {
            issues.Add(CreateIssue(file, null, $"Renderer 缺少穩定渲染證據：{token}。"));
        }
    }

    private string GetMethodRegion(string source, string startToken, string endToken)
    {
        int startIndex = source.IndexOf(startToken, StringComparison.Ordinal);
        int endIndex = source.IndexOf(endToken, StringComparison.Ordinal);
        if (startIndex < 0 || endIndex <= startIndex)
        {
            return string.Empty;
        }

        return source[startIndex..endIndex];
    }

    private GateIssue CreateIssue(string file, int? line, string message)
    {
        return new GateIssue(
            "V3-CODE-RENDER-STABILITY",
            "GameForm",
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
