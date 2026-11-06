using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class CustomMisfitCheckerTests
{
    [Fact]
    public void CurrentProjectCustomCheckersAllPass()
    {
        string projectRoot = FindProjectRoot();
        SpecificationInventory inventory = new SpecificationInventory(
            new List<SpecificationDocument>());
        List<IGateChecker> checkers = new List<IGateChecker>
        {
            new CSharpApplicationWorkflowStructureChecker(projectRoot),
            new CSharpPresenterWorkflowOwnershipChecker(projectRoot),
            new CSharpSnakeMovementTopologyChecker(projectRoot),
            new CSharpRenderStabilityChecker(projectRoot)
        };

        List<GateCheckResult> results = checkers
            .Select(checker => checker.Check(inventory))
            .ToList();

        Assert.All(results, result => Assert.Equal(
            VerificationStatus.Pass,
            result.Status));
    }

    [Fact]
    public void ApplicationWorkflowCheckerRejectsPublicLeafUseCase()
    {
        string temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"application-workflow-checker-{Guid.NewGuid():N}");
        string useCasesPath = Path.Combine(temporaryRoot, "Snake.UseCases");
        Directory.CreateDirectory(useCasesPath);
        File.WriteAllText(
            Path.Combine(useCasesPath, "LegacyUseCase.cs"),
            "public class LegacyUseCase { public void Execute() { } }");
        File.WriteAllText(
            Path.Combine(useCasesPath, "SnakeGameApplicationService.cs"),
            CreateApplicationServiceSource());

        try
        {
            CSharpApplicationWorkflowStructureChecker checker =
                new CSharpApplicationWorkflowStructureChecker(temporaryRoot);

            GateCheckResult result = checker.Check(new SpecificationInventory(
                new List<SpecificationDocument>()));

            Assert.Equal(VerificationStatus.Fail, result.Status);
            Assert.Contains(result.Issues, issue => issue.Message.Contains(
                "葉節點",
                StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    private string CreateApplicationServiceSource()
    {
        return "public class SnakeGameApplicationService { "
            + "public StartCurrentLevelResult StartCurrentLevel() { } "
            + "public SetSnakeDirectionResult SetSnakeDirection() { } "
            + "public ProcessGameTickResult ProcessGameTick() { } "
            + "public RestartGameResult RestartGame() { } "
            + "public GetCurrentScreenResult GetCurrentScreen() { } }";
    }

    private string FindProjectRoot()
    {
        DirectoryInfo? currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory != null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "Snake.sln")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("找不到 Snake.sln。");
    }
}
