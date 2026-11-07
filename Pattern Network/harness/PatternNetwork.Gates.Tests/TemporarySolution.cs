namespace PatternNetwork.Gates.Tests;

public class TemporarySolution : IDisposable
{
    private string _rootPath;
    private string _solutionName;

    public TemporarySolution(string solutionName)
    {
        _solutionName = solutionName;
        _rootPath = Path.Combine(Path.GetTempPath(), $"pattern-network-solution-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootPath);
        WriteProject("Domain", "net8.0", false, new List<string>());
        WriteProject("Application", "net8.0", false, new List<string> { "Domain" });
        WriteProject("Infrastructure", "net8.0", false, new List<string> { "Application" });
        WriteProject("WinForms", "net8.0-windows", true, new List<string> { "Application", "Infrastructure" });
        WriteVerificationProject();
        WriteCurrentSources();
        WriteSolution();
    }

    public string SolutionPath => Path.Combine(_rootPath, $"{_solutionName}.sln");

    public void SetProjectReferences(string patternNodeName, List<string> referencedPatternNodes)
    {
        string normalizedPatternNodeName = NormalizePatternNodeName(patternNodeName);
        List<string> normalizedReferences = referencedPatternNodes
            .Select(NormalizePatternNodeName)
            .ToList();
        string targetFramework = normalizedPatternNodeName == "WinForms" ? "net8.0-windows" : "net8.0";
        WriteProject(
            normalizedPatternNodeName,
            targetFramework,
            normalizedPatternNodeName == "WinForms",
            normalizedReferences);
    }

    public void WriteSource(string patternNodeName, string fileName, string source)
    {
        string normalizedPatternNodeName = NormalizePatternNodeName(patternNodeName);
        string projectDirectory = GetProjectDirectory(normalizedPatternNodeName);
        string relativeSourcePath = GetSourceRelativePath(normalizedPatternNodeName, fileName);
        string sourcePath = Path.Combine(projectDirectory, relativeSourcePath);
        string? sourceDirectory = Path.GetDirectoryName(sourcePath);
        if (!string.IsNullOrWhiteSpace(sourceDirectory))
        {
            Directory.CreateDirectory(sourceDirectory);
        }

        File.WriteAllText(sourcePath, source);
    }

    public void ReplaceProjectText(string patternNodeName, string oldValue, string newValue)
    {
        string normalizedPatternNodeName = NormalizePatternNodeName(patternNodeName);
        string projectName = $"{_solutionName}.{normalizedPatternNodeName}";
        string projectPath = Path.Combine(
            GetProjectDirectory(normalizedPatternNodeName),
            $"{projectName}.csproj");
        string projectText = File.ReadAllText(projectPath);
        File.WriteAllText(
            projectPath,
            projectText.Replace(oldValue, newValue, StringComparison.Ordinal));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, true);
        }
    }

    private void WriteProject(
        string patternNodeName,
        string targetFramework,
        bool useWindowsForms,
        List<string> referencedPatternNodes)
    {
        string projectName = $"{_solutionName}.{patternNodeName}";
        string projectDirectory = GetProjectDirectory(patternNodeName);
        Directory.CreateDirectory(projectDirectory);
        List<string> projectReferences = new List<string>();
        foreach (string referencedPatternNode in referencedPatternNodes)
        {
            string referencedProjectName = $"{_solutionName}.{referencedPatternNode}";
            string referencedProjectPath = Path.Combine(
                GetProjectDirectory(referencedPatternNode),
                $"{referencedProjectName}.csproj");
            string relativeReferencePath = Path.GetRelativePath(projectDirectory, referencedProjectPath);
            projectReferences.Add($"    <ProjectReference Include=\"{relativeReferencePath}\" />");
        }

        string packageReference = patternNodeName switch
        {
            "Application" => "    <PackageReference Include=\"Microsoft.Extensions.DependencyInjection.Abstractions\" Version=\"8.0.1\" />",
            "Infrastructure" => "    <PackageReference Include=\"Microsoft.Extensions.DependencyInjection.Abstractions\" Version=\"8.0.1\" />",
            "WinForms" => "    <PackageReference Include=\"Microsoft.Extensions.DependencyInjection\" Version=\"8.0.1\" />",
            _ => string.Empty
        };
        string projectReferenceGroup = projectReferences.Count == 0
            ? string.Empty
            : $"  <ItemGroup>{Environment.NewLine}{string.Join(Environment.NewLine, projectReferences)}{Environment.NewLine}  </ItemGroup>{Environment.NewLine}";
        string packageReferenceGroup = string.IsNullOrWhiteSpace(packageReference)
            ? string.Empty
            : $"  <ItemGroup>{Environment.NewLine}{packageReference}{Environment.NewLine}  </ItemGroup>{Environment.NewLine}";
        string projectText = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{targetFramework}</TargetFramework>
                <UseWindowsForms>{useWindowsForms.ToString().ToLowerInvariant()}</UseWindowsForms>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            {packageReferenceGroup}{projectReferenceGroup}</Project>
            """;
        File.WriteAllText(Path.Combine(projectDirectory, $"{projectName}.csproj"), projectText);
    }

    private void WriteVerificationProject()
    {
        string projectName = $"{_solutionName}.Test";
        string projectDirectory = GetProjectDirectory("Test");
        Directory.CreateDirectory(projectDirectory);
        List<string> projectReferences = new List<string>();
        foreach (string patternNodeName in new[] { "Domain", "Application", "Infrastructure", "WinForms" })
        {
            string referencedProjectName = $"{_solutionName}.{patternNodeName}";
            string referencedProjectPath = Path.Combine(
                GetProjectDirectory(patternNodeName),
                $"{referencedProjectName}.csproj");
            string relativeReferencePath = Path.GetRelativePath(projectDirectory, referencedProjectPath);
            projectReferences.Add($"    <ProjectReference Include=\"{relativeReferencePath}\" />");
        }

        string projectText = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0-windows</TargetFramework>
                <UseWindowsForms>true</UseWindowsForms>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
            {string.Join(Environment.NewLine, projectReferences)}
              </ItemGroup>
            </Project>
            """;
        File.WriteAllText(Path.Combine(projectDirectory, $"{projectName}.csproj"), projectText);
    }

    private void WriteSolution()
    {
        Dictionary<string, string> folderGuids = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["App"] = "{10000000-0000-0000-0000-000000000001}",
            ["Core"] = "{10000000-0000-0000-0000-000000000002}",
            ["Test"] = "{10000000-0000-0000-0000-000000000003}"
        };
        Dictionary<string, string> projectGuids = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Domain"] = "{00000000-0000-0000-0000-000000000001}",
            ["Application"] = "{00000000-0000-0000-0000-000000000002}",
            ["Infrastructure"] = "{00000000-0000-0000-0000-000000000003}",
            ["WinForms"] = "{00000000-0000-0000-0000-000000000004}",
            ["Test"] = "{00000000-0000-0000-0000-000000000005}"
        };
        List<string> projectLines = new List<string>();
        foreach (KeyValuePair<string, string> folder in folderGuids)
        {
            projectLines.Add($"Project(\"{{2150E333-8FDC-42A3-9474-1A3956D46DE8}}\") = \"{folder.Key}\", \"{folder.Key}\", \"{folder.Value}\"");
            projectLines.Add("EndProject");
        }

        foreach (string patternNodeName in new[] { "Domain", "Application", "Infrastructure", "WinForms", "Test" })
        {
            string projectName = $"{_solutionName}.{patternNodeName}";
            string relativeProjectPath = Path.GetRelativePath(
                _rootPath,
                Path.Combine(GetProjectDirectory(patternNodeName), $"{projectName}.csproj"));
            projectLines.Add($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"{relativeProjectPath}\", \"{projectGuids[patternNodeName]}\"");
            projectLines.Add("EndProject");
        }

        string nestedProjects = $"""
                GlobalSection(NestedProjects) = preSolution
                    {projectGuids["Domain"]} = {folderGuids["Core"]}
                    {projectGuids["Application"]} = {folderGuids["Core"]}
                    {projectGuids["Infrastructure"]} = {folderGuids["Core"]}
                    {projectGuids["WinForms"]} = {folderGuids["App"]}
                    {projectGuids["Test"]} = {folderGuids["Test"]}
                EndGlobalSection
            """;
        string solutionText = $"""
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            {string.Join(Environment.NewLine, projectLines)}
            Global
            {nestedProjects}
            EndGlobal
            """;
        File.WriteAllText(SolutionPath, solutionText);
    }

    private void WriteCurrentSources()
    {
        WriteSource("Domain", "GameSession.cs", $$"""
            namespace {{_solutionName}}.Domain
            {
                public class GameSession
                {
                    private int _score;

                    public GameSession(int score)
                    {
                        _score = score;
                    }

                    public int Score => _score;

                    public void AddScore(int amount)
                    {
                        _score += amount;
                    }
                }
            }
            """);
        WriteSource("Application", "StartGameUseCase.cs", $$"""
            using {{_solutionName}}.Application.Repositories;

            namespace {{_solutionName}}.Application._StartGameUseCase
            {
                public class StartGameUseCase
                {
                    private IGameRepository _gameRepository;

                    public StartGameUseCase(IGameRepository gameRepository)
                    {
                        _gameRepository = gameRepository;
                    }

                    public void StartGame()
                    {
                    }
                }
            }
            """);
        WriteSource("Application", "MoveSnakeUseCase.cs", $$"""
            using {{_solutionName}}.Application.Repositories;

            namespace {{_solutionName}}.Application._MoveSnakeUseCase
            {
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
            }
            """);
        WriteSource("Application", "GameScreenDTO.cs", $$"""
            namespace {{_solutionName}}.Application._GetGameScreenUseCase.Dto
            {
                public class GameScreenDTO
                {
                    public int Score { get; set; }
                }
            }
            """);
        WriteSource("Application", "GetGameScreenUseCase.cs", $$"""
            using {{_solutionName}}.Application.Repositories;
            using {{_solutionName}}.Application._GetGameScreenUseCase.Dto;

            namespace {{_solutionName}}.Application._GetGameScreenUseCase
            {
                public class GetGameScreenUseCase
                {
                    private IGameRepository _gameRepository;

                    public GetGameScreenUseCase(IGameRepository gameRepository)
                    {
                        _gameRepository = gameRepository;
                    }

                    public GameScreenDTO GetGameScreen()
                    {
                        return new GameScreenDTO();
                    }
                }
            }
            """);
        WriteSource("Application", "Repositories/IGameRepository.cs", $$"""
            using {{_solutionName}}.Domain;

            namespace {{_solutionName}}.Application.Repositories
            {
                public interface IGameRepository
                {
                    GameSession GetCurrentGame();
                }
            }
            """);
        WriteSource("Application", "DependencyInjection.cs", $$"""
            using Microsoft.Extensions.DependencyInjection;
            using {{_solutionName}}.Application._GetGameScreenUseCase;
            using {{_solutionName}}.Application._MoveSnakeUseCase;
            using {{_solutionName}}.Application._StartGameUseCase;

            namespace {{_solutionName}}.Application
            {
                public static class DependencyInjection
                {
                    public static IServiceCollection AddApplication(this IServiceCollection services)
                    {
                        services.AddTransient<StartGameUseCase>();
                        services.AddTransient<MoveSnakeUseCase>();
                        services.AddTransient<GetGameScreenUseCase>();
                        return services;
                    }
                }
            }
            """);
        WriteSource("Infrastructure", "Repositories/_Game/GameRepository.cs", $$"""
            using {{_solutionName}}.Application.Repositories;
            using {{_solutionName}}.Domain;

            namespace {{_solutionName}}.Infrastructure.Repositories._Game
            {
                public class GameRepository : IGameRepository
                {
                    private GameSession _game;

                    public GameRepository()
                    {
                        _game = new GameSession(0);
                    }

                    public GameSession GetCurrentGame()
                    {
                        return _game;
                    }
                }
            }
            """);
        WriteSource("Infrastructure", "DependencyInjection.cs", $$"""
            using Microsoft.Extensions.DependencyInjection;
            using {{_solutionName}}.Application.Repositories;
            using {{_solutionName}}.Infrastructure.Repositories._Game;

            namespace {{_solutionName}}.Infrastructure
            {
                public static class DependencyInjection
                {
                    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
                    {
                        services.AddSingleton<IGameRepository, GameRepository>();
                        return services;
                    }
                }
            }
            """);
        WriteSource("WinForms", "GameForm.cs", $$"""
            using System.Windows.Forms;
            using {{_solutionName}}.Application._GetGameScreenUseCase;
            using {{_solutionName}}.Application._GetGameScreenUseCase.Dto;
            using {{_solutionName}}.Application._MoveSnakeUseCase;
            using {{_solutionName}}.Application._StartGameUseCase;

            namespace {{_solutionName}}.WinForms
            {
                public class GameForm : Form
                {
                    private StartGameUseCase _startGameUseCase;
                    private MoveSnakeUseCase _moveSnakeUseCase;
                    private GetGameScreenUseCase _getGameScreenUseCase;
                    private System.Windows.Forms.Timer _gameTimer;

                    public GameForm(
                        StartGameUseCase startGameUseCase,
                        MoveSnakeUseCase moveSnakeUseCase,
                        GetGameScreenUseCase getGameScreenUseCase)
                    {
                        _startGameUseCase = startGameUseCase;
                        _moveSnakeUseCase = moveSnakeUseCase;
                        _getGameScreenUseCase = getGameScreenUseCase;
                        _gameTimer = new System.Windows.Forms.Timer
                        {
                            Interval = 250
                        };
                    }

                    private void ExecuteGameTick()
                    {
                        _moveSnakeUseCase.MoveSnake();
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
        WriteSource("WinForms", "Program.cs", $$"""
            using Microsoft.Extensions.DependencyInjection;
            using {{_solutionName}}.Application;
            using {{_solutionName}}.Infrastructure;

            namespace {{_solutionName}}.WinForms
            {
                internal static class Program
                {
                    [STAThread]
                    static void Main()
                    {
                        ApplicationConfiguration.Initialize();
                        ServiceCollection services = new ServiceCollection();
                        services.AddApplication();
                        services.AddInfrastructure();
                        services.AddTransient<GameForm>();
                        using ServiceProvider serviceProvider = services.BuildServiceProvider();
                        GameForm form = serviceProvider.GetRequiredService<GameForm>();
                        Application.Run(form);
                    }
                }
            }
            """);
    }

    private void WriteValidSources()
    {
        WriteSource("Domain", "GameSession.cs", $$"""
            namespace {{_solutionName}}.Domain;

            public class GameSession
            {
                private int _score;

                public GameSession(int score)
                {
                    _score = score;
                }

                public int Score => _score;

                public void AddScore(int amount)
                {
                    _score += amount;
                }
            }
            """);
        WriteSource("Application", "StartGameCommand.cs", $$"""
            namespace {{_solutionName}}.Application._Game.Dto;

            public class StartGameCommand
            {
            }
            """);
        WriteSource("Application", "StartGameResult.cs", $$"""
            namespace {{_solutionName}}.Application._Game.Dto;

            public class StartGameResult
            {
                public bool IsSuccess { get; set; }

                public string? FailureCode { get; set; }

                public string? FailureMessage { get; set; }
            }
            """);
        WriteSource("Application", "IStartGameUseCase.cs", $$"""
            using {{_solutionName}}.Application._Game.Dto;

            namespace {{_solutionName}}.Application._Game.Interface;

            public interface IStartGameUseCase
            {
                StartGameResult StartGame(StartGameCommand command);
            }
            """);
        WriteSource("Application", "StartGameUseCase.cs", $$"""
            using {{_solutionName}}.Application._Game.Dto;
            using {{_solutionName}}.Application._Game.Interface;

            namespace {{_solutionName}}.Application._Game;

            public class StartGameUseCase : IStartGameUseCase
            {
                public StartGameResult StartGame(StartGameCommand command)
                {
                    return new StartGameResult
                    {
                        IsSuccess = true
                    };
                }
            }
            """);
        WriteSource("Application", "DependencyInjection.cs", $$"""
            using Microsoft.Extensions.DependencyInjection;
            using {{_solutionName}}.Application._Game;
            using {{_solutionName}}.Application._Game.Interface;

            namespace {{_solutionName}}.Application;

            public static class DependencyInjection
            {
                public static IServiceCollection AddApplication(this IServiceCollection services)
                {
                    services.AddTransient<IStartGameUseCase, StartGameUseCase>();
                    return services;
                }
            }
            """);
        WriteSource("WinForms", "IGameView.cs", $$"""
            namespace {{_solutionName}}.WinForms;

            public interface IGameView
            {
                void ShowStarted();
            }
            """);
        WriteSource("WinForms", "GamePresenter.cs", $$"""
            using {{_solutionName}}.Application._Game.Dto;
            using {{_solutionName}}.Application._Game.Interface;

            namespace {{_solutionName}}.WinForms;

            public class GamePresenter
            {
                private IGameView _view;
                private IStartGameUseCase _startGameUseCase;

                public GamePresenter(IGameView view, IStartGameUseCase startGameUseCase)
                {
                    _view = view;
                    _startGameUseCase = startGameUseCase;
                }

                public void Start()
                {
                    StartGameResult result = _startGameUseCase.StartGame(new StartGameCommand());
                    if (result.IsSuccess)
                    {
                        _view.ShowStarted();
                    }
                }
            }
            """);
        WriteSource("WinForms", "GameForm.cs", $$"""
            using System.Windows.Forms;

            namespace {{_solutionName}}.WinForms;

            public class GameForm : Form, IGameView
            {
                private GamePresenter _presenter;

                public GameForm(GamePresenter presenter)
                {
                    _presenter = presenter;
                }

                public void ShowStarted()
                {
                    Text = "Started";
                }
            }
            """);
        WriteSource("WinForms", "Program.cs", $$"""
            using Microsoft.Extensions.DependencyInjection;
            using {{_solutionName}}.Application;
            using {{_solutionName}}.WinForms;

            ApplicationConfiguration.Initialize();
            ServiceCollection services = new ServiceCollection();
            services.AddApplication();
            services.AddSingleton<GameForm>();
            services.AddSingleton<IGameView>(serviceProvider =>
                serviceProvider.GetRequiredService<GameForm>());
            services.AddSingleton<GamePresenter>();
            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            GamePresenter presenter = serviceProvider.GetRequiredService<GamePresenter>();
            GameForm form = serviceProvider.GetRequiredService<GameForm>();
            presenter.Start();
            Application.Run(form);
            """);
    }

    private string NormalizePatternNodeName(string patternNodeName)
    {
        return patternNodeName switch
        {
            "Entities" => "Domain",
            "UseCases" => "Application",
            _ => patternNodeName
        };
    }

    private string GetProjectDirectory(string patternNodeName)
    {
        string solutionFolder = patternNodeName switch
        {
            "Domain" => "Core",
            "Application" => "Core",
            "Infrastructure" => "Core",
            "WinForms" => "App",
            "Test" => "Test",
            _ => throw new InvalidOperationException($"不支援的 project node：{patternNodeName}")
        };
        return Path.Combine(_rootPath, solutionFolder, $"{_solutionName}.{patternNodeName}");
    }

    private string GetSourceRelativePath(string patternNodeName, string fileName)
    {
        if (fileName.Contains("/", StringComparison.Ordinal)
            || fileName.Contains("\\", StringComparison.Ordinal))
        {
            return fileName.Replace('/', Path.DirectorySeparatorChar);
        }

        if (patternNodeName != "Application" || fileName == "DependencyInjection.cs")
        {
            return fileName;
        }

        string fileTypeName = Path.GetFileNameWithoutExtension(fileName);
        if (fileTypeName.EndsWith("UseCase", StringComparison.Ordinal))
        {
            return Path.Combine($"_{fileTypeName}", fileName);
        }

        if (fileTypeName == "GameScreenDTO")
        {
            return Path.Combine("_GetGameScreenUseCase", "Dto", fileName);
        }

        if (fileTypeName.EndsWith("DTO", StringComparison.Ordinal))
        {
            return Path.Combine("_StartGameUseCase", "Dto", fileName);
        }

        return Path.Combine("_StartGameUseCase", fileName);
    }
}
