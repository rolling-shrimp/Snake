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
        Directory.Delete(Path.Combine(
            Path.GetDirectoryName(solution.SolutionPath)!,
            "Core",
            "SampleGame.Domain"), true);
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

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-DOM-PRO-004");
    }

    [Fact]
    public void CheckCodeRejectsStandaloneDomainIdClass()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Domain", "BoardId.cs", """
            namespace SampleGame.Domain
            {
                public class BoardId
                {
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-DOM-PRO-007");
    }

    [Fact]
    public void CheckCodeRejectsDomainEnumOutsideEnumsDirectory()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Domain", "Direction.cs", """
            namespace SampleGame.Domain
            {
                public enum Direction
                {
                    Right
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-DOM-PRO-008");
    }

    [Fact]
    public void CheckCodeRejectsUseCaseEnumOutsideEnumsDirectory()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Application", "GameAction.cs", """
            namespace SampleGame.Application._StartGameUseCase
            {
                public enum GameAction
                {
                    Start
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UC-PRO-010");
    }

    [Fact]
    public void CheckCodeRejectsLegacyGameStateDirectory()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Application", "_GameState/GameState.cs", """
            namespace SampleGame.Application._GameState
            {
                public class GameState
                {
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UC-PRO-009");
    }

    [Fact]
    public void CheckCodeRejectsTransientGameRepository()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Infrastructure", "DependencyInjection.cs", """
            using Microsoft.Extensions.DependencyInjection;
            using SampleGame.Application.Repositories;
            using SampleGame.Infrastructure.Repositories._Game;

            namespace SampleGame.Infrastructure
            {
                public static class DependencyInjection
                {
                    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
                    {
                        services.AddTransient<IGameRepository, GameRepository>();
                        return services;
                    }
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-INF-PRO-002");
    }

    [Fact]
    public void CheckCodeRejectsCompositionRootWithoutInfrastructureRegistration()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("WinForms", "Program.cs", """
            using Microsoft.Extensions.DependencyInjection;
            using SampleGame.Application;

            namespace SampleGame.WinForms
            {
                internal static class Program
                {
                    [STAThread]
                    static void Main()
                    {
                        ApplicationConfiguration.Initialize();
                        ServiceCollection services = new ServiceCollection();
                        services.AddApplication();
                        services.AddTransient<GameForm>();
                        using ServiceProvider serviceProvider = services.BuildServiceProvider();
                        GameForm form = serviceProvider.GetRequiredService<GameForm>();
                        Application.Run(form);
                    }
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UI-010"
            && issue.Message.Contains("AddInfrastructure", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckCodeAcceptsNaturalUseCaseParameterWithoutCommandOrQuerySuffix()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("UseCases", "StartGameUseCase.cs", """
            namespace SampleGame.Application._StartGameUseCase
            {
                public class StartGameUseCase
                {
                    public bool StartGame(string playerName)
                    {
                        return playerName.Length > 0;
                    }
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.DoesNotContain(result.Issues, issue => issue.RuleId == "V3-PN-UC-PRO-003");
    }

    [Fact]
    public void CheckCodeRejectsForbiddenPresenterRole()
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

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UI-PRO-007");
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
            using SampleGame.Domain;

            namespace SampleGame.Application._Game.Dto;

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
    public void CheckCodeRejectsUseCaseWithMultipleWorkflowMethods()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("UseCases", "StartGameUseCase.cs", """
            namespace SampleGame.Application._StartGameUseCase
            {
                public class StartGameUseCase
                {
                    public void StartGame()
                    {
                    }

                    public void RestartGame()
                    {
                    }
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UC-007");
    }

    [Fact]
    public void CheckCodeRejectsLegacyExecuteUseCaseAsBlockingViolation()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Application", "StartGameUseCase.cs", """
            namespace SampleGame.Application._StartGameUseCase
            {
                public class StartGameUseCase
                {
                    public void Execute()
                    {
                    }
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UC-007");
        Assert.Contains(result.Checks, check =>
            check.Mode == VerificationMode.Blocking
            && check.Status == VerificationStatus.Fail);
    }

    [Fact]
    public void CheckCodeRejectsDirectUseCaseConstruction()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Application", "UseCaseFactory.cs", """
            using SampleGame.Application._Game;

            namespace SampleGame.Application._Game;

            public class UseCaseFactory
            {
                public StartGameUseCase Create()
                {
                    return new StartGameUseCase();
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-CS-DI-002");
    }

    [Fact]
    public void CheckCodeRejectsServiceLocator()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Application", "UseCaseResolver.cs", """
            using Microsoft.Extensions.DependencyInjection;
            using SampleGame.Application._Game.Interface;

            namespace SampleGame.Application._Game;

            public class UseCaseResolver
            {
                private IServiceProvider _serviceProvider;

                public UseCaseResolver(IServiceProvider serviceProvider)
                {
                    _serviceProvider = serviceProvider;
                }

                public IStartGameUseCase Resolve()
                {
                    return _serviceProvider.GetRequiredService<IStartGameUseCase>();
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-CS-DI-003");
    }

    [Fact]
    public void CheckCodeRejectsWrongUseCaseLifetime()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("Application", "DependencyInjection.cs", """
            using Microsoft.Extensions.DependencyInjection;
            using SampleGame.Application._Game;
            using SampleGame.Application._Game.Interface;

            namespace SampleGame.Application;

            public static class DependencyInjection
            {
                public static IServiceCollection AddApplication(this IServiceCollection services)
                {
                    services.AddSingleton<IStartGameUseCase, StartGameUseCase>();
                    return services;
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UC-009");
    }

    [Fact]
    public void CheckCodeRejectsWrongDependencyInjectionPackageVersion()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.ReplaceProjectText("Application", "Version=\"8.0.1\"", "Version=\"8.0.0\"");
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.False(result.IsSuccessful);
        Assert.Contains(result.Issues, issue =>
            issue.RuleId == "V3-PN-ARCH-001"
            && issue.Message.Contains("Microsoft.Extensions.DependencyInjection.Abstractions@8.0.1", StringComparison.Ordinal));
    }

    [Fact]
    public void CheckCodeAllowsGameFormToOrchestrateMultipleUseCases()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.WriteSource("WinForms", "GameForm.cs", """
            using System.Windows.Forms;
            using SampleGame.Application._GetGameScreenUseCase;
            using SampleGame.Application._GetGameScreenUseCase.Dto;
            using SampleGame.Application._MoveSnakeUseCase;
            using SampleGame.Application._StartGameUseCase;

            namespace SampleGame.WinForms
            {
                public class GameForm : Form
                {
                    private StartGameUseCase _startGameUseCase;
                    private MoveSnakeUseCase _moveSnakeUseCase;
                    private GetGameScreenUseCase _getGameScreenUseCase;

                    public GameForm(
                        StartGameUseCase startGameUseCase,
                        MoveSnakeUseCase moveSnakeUseCase,
                        GetGameScreenUseCase getGameScreenUseCase)
                    {
                        _startGameUseCase = startGameUseCase;
                        _moveSnakeUseCase = moveSnakeUseCase;
                        _getGameScreenUseCase = getGameScreenUseCase;
                    }

                    private void Start()
                    {
                        _startGameUseCase.StartGame();
                        ApplyScreen(_getGameScreenUseCase.GetGameScreen());
                    }

                    private void ExecuteGameTick()
                    {
                        _moveSnakeUseCase.MoveSnake();
                        ApplyScreen(_getGameScreenUseCase.GetGameScreen());
                    }

                    private void ApplyScreen(GameScreenDTO screen)
                    {
                        if (HasCellChanged(screen))
                        {
                            Text = screen.Score.ToString();
                        }
                    }

                    private bool HasCellChanged(GameScreenDTO screen)
                    {
                        return screen.Score >= 0;
                    }
                }
            }
            """);
        CodeGateRunner runner = new CodeGateRunner();

        GateRunResult result = runner.CheckCode(FindPatternNetworkPath(), solution.SolutionPath, null);

        Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
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
        Assert.All(
            result.Issues.Where(issue => issue.Message.Contains("方向輸入", StringComparison.Ordinal)),
            issue => Assert.Equal("V3-PN-UI-008", issue.RuleId));
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

        Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-UI-008");
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
