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
        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-ARCH-001" && issue.Node == "winforms");
    }

    [Fact]
    public void CheckCodeReportsConfigurationErrorWhenDerivedProjectIsMissing()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        Directory.Delete(Path.Combine(Path.GetDirectoryName(solution.SolutionPath)!, "SampleGame.Entities"), true);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.True(result.HasConfigurationError);
        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-TARGET-NOT-FOUND");
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

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-CS-FLOW-001");
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

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-ENT-PRO-004");
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

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UC-PRO-003");
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

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UI-PRO-002");
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

        Assert.Single(result.Issues, issue => issue.RuleId == "V3-PN-CS-CLASS-001" && issue.Message.Contains("partial", StringComparison.Ordinal));
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

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-CS-DI-001");
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

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UC-PRO-005");
    }

    [Fact]
    public void CheckCodeAllowsApplicationServiceWithMultipleWorkflowMethods()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("UseCases", "StartGameUseCase.cs", """
            namespace SampleGame.UseCases;

            public class StartGameUseCase
            {
                public StartGameResult StartGame(StartGameCommand command)
                {
                    return new StartGameResult
                    {
                        IsSuccess = true
                    };
                }

                public StartGameResult RestartGame(StartGameCommand command)
                {
                    return new StartGameResult
                    {
                        IsSuccess = true
                    };
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.DoesNotContain(result.Issues, issue =>
            issue.Message.Contains("有且只有一個 public Execute", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckCodeReportsLegacySingleExecuteUseCaseAsNonBlockingAudit()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.True(result.IsSuccessful);
        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UC-PRO-006");
        Assert.Contains(result.Checks, check =>
            check.Mode == VerificationMode.Audit
            && check.Status == VerificationStatus.Fail);
    }

    [Fact]
    public void CheckCodeReportsPresenterThatOrchestratesMultipleUseCasesAsNonBlockingAudit()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("WinForms", "GamePresenter.cs", """
            using SampleGame.UseCases;

            namespace SampleGame.WinForms;

            public class GamePresenter
            {
                private IGameView _view;
                private StartGameUseCase _startGameUseCase;
                private StartGameUseCase _restartGameUseCase;

                public GamePresenter(
                    IGameView view,
                    StartGameUseCase startGameUseCase,
                    StartGameUseCase restartGameUseCase)
                {
                    _view = view;
                    _startGameUseCase = startGameUseCase;
                    _restartGameUseCase = restartGameUseCase;
                }

                public void Start()
                {
                    _startGameUseCase.Execute(new StartGameCommand());
                    _restartGameUseCase.Execute(new StartGameCommand());
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.True(result.IsSuccessful);
        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UI-PRO-005");
        Assert.Contains(result.Checks, check =>
            check.Mode == VerificationMode.Audit
            && check.Status == VerificationStatus.Fail);
    }

    [Fact]
    public void CheckCodeAcceptsDirectionInputHandledByProcessCmdKey()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("WinForms", "GameForm.cs", """
            using System.Windows.Forms;

            namespace SampleGame.WinForms;

            public class GameForm : Form
            {
                public event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;

                protected override bool ProcessCmdKey(ref Message message, Keys keyData)
                {
                    Direction? direction = (keyData & Keys.KeyCode) switch
                    {
                        Keys.Up => Direction.Up,
                        Keys.Down => Direction.Down,
                        Keys.Left => Direction.Left,
                        Keys.Right => Direction.Right,
                        _ => null
                    };
                    if (direction is null)
                    {
                        return base.ProcessCmdKey(ref message, keyData);
                    }

                    DirectionRequested?.Invoke(this, new DirectionRequestedEventArgs(direction.Value));
                    return true;
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.DoesNotContain(result.Issues, issue =>
            issue.RuleId == "V3-PN-UI-008"
            || issue.RuleId == "V3-PN-UI-PRO-007");
    }

    [Fact]
    public void CheckCodeRejectsDirectionInputHandledOnlyByKeyDown()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("WinForms", "GameForm.cs", """
            using System.Windows.Forms;

            namespace SampleGame.WinForms;

            public class GameForm : Form
            {
                public event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;

                private void OnGameKeyDown(object? sender, KeyEventArgs eventArgs)
                {
                    Direction? direction = eventArgs.KeyCode switch
                    {
                        Keys.Up => Direction.Up,
                        Keys.Down => Direction.Down,
                        Keys.Left => Direction.Left,
                        Keys.Right => Direction.Right,
                        _ => null
                    };
                    if (direction is not null)
                    {
                        DirectionRequested?.Invoke(this, new DirectionRequestedEventArgs(direction.Value));
                    }
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UI-008");
        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UI-PRO-007");
    }

    [Fact]
    public void CheckCodeRejectsIncompleteProcessCmdKeyDirectionMapping()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("WinForms", "GameForm.cs", """
            using System.Windows.Forms;

            namespace SampleGame.WinForms;

            public class GameForm : Form
            {
                public event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;

                protected override bool ProcessCmdKey(ref Message message, Keys keyData)
                {
                    Direction? direction = (keyData & Keys.KeyCode) switch
                    {
                        Keys.Up => Direction.Up,
                        Keys.Down => Direction.Down,
                        Keys.Left => Direction.Left,
                        _ => null
                    };
                    if (direction is null)
                    {
                        return base.ProcessCmdKey(ref message, keyData);
                    }

                    DirectionRequested?.Invoke(this, new DirectionRequestedEventArgs(direction.Value));
                    return true;
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UI-008");
    }

    [Fact]
    public void CheckCodeRejectsParallelKeyDownDirectionDispatch()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("WinForms", "GameForm.cs", """
            using System.Windows.Forms;

            namespace SampleGame.WinForms;

            public class GameForm : Form
            {
                public event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;

                protected override bool ProcessCmdKey(ref Message message, Keys keyData)
                {
                    Direction? direction = (keyData & Keys.KeyCode) switch
                    {
                        Keys.Up => Direction.Up,
                        Keys.Down => Direction.Down,
                        Keys.Left => Direction.Left,
                        Keys.Right => Direction.Right,
                        _ => null
                    };
                    if (direction is null)
                    {
                        return base.ProcessCmdKey(ref message, keyData);
                    }

                    DirectionRequested?.Invoke(this, new DirectionRequestedEventArgs(direction.Value));
                    return true;
                }

                private void OnGameKeyDown(object? sender, KeyEventArgs eventArgs)
                {
                    if (eventArgs.KeyCode == Keys.Up)
                    {
                        DirectionRequested?.Invoke(this, new DirectionRequestedEventArgs(Direction.Up));
                    }
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UI-PRO-007");
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
