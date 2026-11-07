using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class CustomMisfitCheckerTests
{
    [Fact]
    public void ApplicationUseCaseCheckerAcceptsConcreteUseCaseWithNaturalContract()
    {
        string temporaryRoot = CreateApplicationProject(
            "_MoveSnakeUseCase",
            "MoveSnakeUseCase.cs",
            """
            public class MoveSnakeUseCase
            {
                private IGameRepository _gameRepository;

                public MoveSnakeUseCase(IGameRepository gameRepository)
                {
                    _gameRepository = gameRepository;
                }

                public void MoveSnake()
                {
                }
            }
            """);

        try
        {
            CSharpApplicationUseCaseStructureChecker checker =
                new CSharpApplicationUseCaseStructureChecker(temporaryRoot);

            GateCheckResult result = checker.Check(new SpecificationInventory(
                new List<SpecificationDocument>()));

            Assert.Equal(VerificationStatus.Pass, result.Status);
        }
        finally
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    [Fact]
    public void ApplicationUseCaseCheckerRejectsUseCaseWithoutGameRepositoryInjection()
    {
        string temporaryRoot = CreateApplicationProject(
            "_MoveSnakeUseCase",
            "MoveSnakeUseCase.cs",
            """
            public class MoveSnakeUseCase
            {
                public void MoveSnake()
                {
                }
            }
            """);

        try
        {
            CSharpApplicationUseCaseStructureChecker checker =
                new CSharpApplicationUseCaseStructureChecker(temporaryRoot);

            GateCheckResult result = checker.Check(new SpecificationInventory(
                new List<SpecificationDocument>()));

            Assert.Equal(VerificationStatus.Fail, result.Status);
            Assert.Contains(result.Issues, issue => issue.Message.Contains(
                "IGameRepository",
                StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    [Fact]
    public void ApplicationWorkflowCheckerRejectsApplicationService()
    {
        string temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"application-workflow-checker-{Guid.NewGuid():N}");
        string applicationPath = Path.Combine(temporaryRoot, "Core", "Snake.Application", "_SnakeGame");
        Directory.CreateDirectory(applicationPath);
        File.WriteAllText(
            Path.Combine(applicationPath, "SnakeGameApplicationService.cs"),
            CreateApplicationServiceSource());

        try
        {
            CSharpApplicationUseCaseStructureChecker checker =
                new CSharpApplicationUseCaseStructureChecker(temporaryRoot);

            GateCheckResult result = checker.Check(new SpecificationInventory(
                new List<SpecificationDocument>()));

            Assert.Equal(VerificationStatus.Fail, result.Status);
            Assert.Contains(result.Issues, issue => issue.Message.Contains(
                "ApplicationService",
                StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    [Fact]
    public void GameFormBoundaryCheckerRejectsMultipleMoveCallsInOneTick()
    {
        string temporaryRoot = CreateWinFormsSource(
            "GameForm.cs",
            """
            public class GameForm
            {
                private MoveSnakeUseCase _moveSnakeUseCase;

                public GameForm(MoveSnakeUseCase moveSnakeUseCase)
                {
                    _moveSnakeUseCase = moveSnakeUseCase;
                }

                private void ExecuteGameTick()
                {
                    _moveSnakeUseCase.MoveSnake();
                    _moveSnakeUseCase.MoveSnake();
                }

                private void ApplyScreen()
                {
                    HasCellChanged();
                }
            }
            """);

        try
        {
            PatternNodeDefinition winForms = LoadWinFormsPattern();
            CSharpGateInspector inspector = new CSharpGateInspector();
            List<GateIssue> issues = inspector.Inspect(
                winForms,
                Path.Combine(temporaryRoot, "App", "Snake.WinForms"));

            Assert.Contains(issues, issue => issue.Message.Contains(
                "一次",
                StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(temporaryRoot, true);
        }
    }

    [Fact]
    public void WinFormsRoleCheckerRejectsGameRender()
    {
        string temporaryRoot = CreateWinFormsSource(
            "GameRender.cs",
            """
            public class GameRender
            {
                private MoveSnakeUseCase _moveSnakeUseCase;

                public GameRender(MoveSnakeUseCase moveSnakeUseCase)
                {
                    _moveSnakeUseCase = moveSnakeUseCase;
                }

                public void Render(GameScreenDTO screen)
                {
                    _moveSnakeUseCase.MoveSnake();
                }
            }
            """);

        try
        {
            PatternNodeDefinition winForms = LoadWinFormsPattern();
            CSharpGateInspector inspector = new CSharpGateInspector();
            List<GateIssue> issues = inspector.Inspect(
                winForms,
                Path.Combine(temporaryRoot, "App", "Snake.WinForms"));

            Assert.Contains(issues, issue => issue.RuleId == "V3-PN-UI-PRO-002");
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

    private string CreateApplicationProject(
        string featureDirectory,
        string fileName,
        string source)
    {
        string temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"application-use-case-checker-{Guid.NewGuid():N}");
        string applicationPath = Path.Combine(
            temporaryRoot,
            "Core",
            "Snake.Application",
            featureDirectory);
        Directory.CreateDirectory(applicationPath);
        File.WriteAllText(Path.Combine(applicationPath, fileName), source);
        return temporaryRoot;
    }

    private string CreateWinFormsSource(string fileName, string source)
    {
        string temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            $"winforms-role-checker-{Guid.NewGuid():N}");
        string winFormsPath = Path.Combine(
            temporaryRoot,
            "App",
            "Snake.WinForms");
        Directory.CreateDirectory(winFormsPath);
        File.WriteAllText(Path.Combine(winFormsPath, fileName), source);
        return temporaryRoot;
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

    private PatternNodeDefinition LoadWinFormsPattern()
    {
        PatternNetworkValidator validator = new PatternNetworkValidator();
        PatternNetworkValidationResult result = validator.Validate(
            Path.Combine(FindProjectRoot(), "Pattern Network"));
        return Assert.Single(result.Definition.PatternNodes, node => node.Id == "winforms");
    }
}
