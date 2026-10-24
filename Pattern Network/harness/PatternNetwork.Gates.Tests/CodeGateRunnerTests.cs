using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class CodeGateRunnerTests
{
    [Fact]
    public void CheckCodeAcceptsNamesAndReferencesDerivedFromTheSolution()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
    }

    [Fact]
    public void CheckCodeRejectsWinFormsDirectReferenceToEntities()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.SetProjectReferences("WinForms", new List<string> { "UseCases", "Entities" });
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.Issues, issue => issue.RuleId == "PN-ARCH-001" && issue.Node == "winforms");
    }

    [Fact]
    public void CheckCodeReportsConfigurationErrorWhenDerivedProjectIsMissing()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        Directory.Delete(Path.Combine(Path.GetDirectoryName(solution.SolutionPath)!, "SampleGame.Entities"), true);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.True(result.HasConfigurationError);
        Assert.Contains(result.Issues, issue => issue.RuleId == "PN-TARGET-NOT-FOUND");
    }

    [Fact]
    public void CheckCodeRejectsControlFlowWithoutBraces()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Entities", "GameSession.cs", """
            namespace SampleGame.Entities;

            public class GameSession
            {
                private int _score;

                public void AddScore(int amount)
                {
                    if (amount > 0)
                        _score += amount;
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "PN-CS-FLOW-001");
    }

    [Fact]
    public void CheckCodeRejectsPublicEntitySetter()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Entities", "GameSession.cs", """
            namespace SampleGame.Entities;

            public class GameSession
            {
                public int Score { get; set; }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "PN-ENT-PRO-004");
    }

    [Fact]
    public void CheckCodeRejectsUseCaseInputWithoutCommandOrQuerySuffix()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("UseCases", "StartGameUseCase.cs", """
            namespace SampleGame.UseCases;

            public class StartGameUseCase
            {
                public StartGameResult Execute(StartGameRequest request)
                {
                    return new StartGameResult();
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "PN-UC-PRO-003");
    }

    [Fact]
    public void CheckCodeRejectsPresenterThatStoresAWinFormsControl()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("WinForms", "GamePresenter.cs", """
            using System.Windows.Forms;

            namespace SampleGame.WinForms;

            public class GamePresenter
            {
                private Form _form;

                public GamePresenter(Form form)
                {
                    _form = form;
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "PN-UI-PRO-002");
    }

    [Fact]
    public void CheckCodeAllowsGeneratedPartialButRejectsHandwrittenPartial()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("WinForms", "GameForm.Designer.cs", """
            namespace SampleGame.WinForms;

            partial class GameForm
            {
            }
            """);
        solution.WriteSource("WinForms", "HandWrittenPart.cs", """
            namespace SampleGame.WinForms;

            public partial class HandWrittenPart
            {
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Single(result.Issues, issue => issue.RuleId == "PN-CS-CLASS-001" && issue.Message.Contains("partial", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckCodeRejectsDependencyFieldThatDoesNotMatchConstructorParameter()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("WinForms", "GamePresenter.cs", """
            using SampleGame.UseCases;

            namespace SampleGame.WinForms;

            public class GamePresenter
            {
                private IGameView _screen;
                private StartGameUseCase _startGameUseCase;

                public GamePresenter(IGameView view, StartGameUseCase startGameUseCase)
                {
                    _screen = view;
                    _startGameUseCase = startGameUseCase;
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "PN-CS-DI-001");
    }

    [Fact]
    public void CheckCodeRejectsEntityTypeExposedByUseCaseResult()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("UseCases", "StartGameResult.cs", """
            using SampleGame.Entities;

            namespace SampleGame.UseCases;

            public class StartGameResult
            {
                public bool IsSuccess { get; set; }

                public string? FailureCode { get; set; }

                public string? FailureMessage { get; set; }

                public GameSession Session { get; set; } = null!;
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "PN-UC-PRO-005");
    }

    private string FindPatternNetworkPath()
    {
        DirectoryInfo? currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory != null)
        {
            string candidate = Path.Combine(currentDirectory.FullName, "Pattern Network", "doctrine.yaml");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("找不到 Pattern Network/doctrine.yaml。");
    }
}
