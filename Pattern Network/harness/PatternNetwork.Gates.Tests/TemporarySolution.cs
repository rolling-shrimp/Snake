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
        WriteProject("Entities", "net8.0", false, new List<string>());
        WriteProject("UseCases", "net8.0", false, new List<string> { "Entities" });
        WriteProject("WinForms", "net8.0-windows", true, new List<string> { "UseCases" });
        WriteValidSources();
        WriteSolution();
    }

    public string SolutionPath => Path.Combine(_rootPath, $"{_solutionName}.sln");

    public void SetProjectReferences(string patternNodeName, List<string> referencedPatternNodes)
    {
        string targetFramework = patternNodeName == "WinForms" ? "net8.0-windows" : "net8.0";
        WriteProject(patternNodeName, targetFramework, patternNodeName == "WinForms", referencedPatternNodes);
    }

    public void WriteSource(string patternNodeName, string fileName, string source)
    {
        string projectName = $"{_solutionName}.{patternNodeName}";
        string projectDirectory = Path.Combine(_rootPath, projectName);
        File.WriteAllText(Path.Combine(projectDirectory, fileName), source);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, true);
        }
    }

    private void WriteProject(string patternNodeName, string targetFramework, bool useWindowsForms, List<string> referencedPatternNodes)
    {
        string projectName = $"{_solutionName}.{patternNodeName}";
        string projectDirectory = Path.Combine(_rootPath, projectName);
        Directory.CreateDirectory(projectDirectory);
        List<string> projectReferences = new List<string>();
        foreach (string referencedPatternNode in referencedPatternNodes)
        {
            string referencedProjectName = $"{_solutionName}.{referencedPatternNode}";
            projectReferences.Add($"    <ProjectReference Include=\"..\\{referencedProjectName}\\{referencedProjectName}.csproj\" />");
        }

        string projectReferenceGroup = projectReferences.Count == 0
            ? string.Empty
            : $"  <ItemGroup>{Environment.NewLine}{string.Join(Environment.NewLine, projectReferences)}{Environment.NewLine}  </ItemGroup>{Environment.NewLine}";
        string projectText = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>{targetFramework}</TargetFramework>
                <UseWindowsForms>{useWindowsForms.ToString().ToLowerInvariant()}</UseWindowsForms>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            {projectReferenceGroup}</Project>
            """;
        File.WriteAllText(Path.Combine(projectDirectory, $"{projectName}.csproj"), projectText);
    }

    private void WriteSolution()
    {
        List<string> projectLines = new List<string>();
        int projectIndex = 1;
        foreach (string patternNodeName in new[] { "Entities", "UseCases", "WinForms" })
        {
            string projectName = $"{_solutionName}.{patternNodeName}";
            projectLines.Add($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{projectName}\", \"{projectName}\\{projectName}.csproj\", \"{{00000000-0000-0000-0000-{projectIndex:000000000000}}}\"");
            projectLines.Add("EndProject");
            projectIndex++;
        }

        string solutionText = $"""

            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            {string.Join(Environment.NewLine, projectLines)}
            Global
            EndGlobal
            """;
        File.WriteAllText(SolutionPath, solutionText);
    }

    private void WriteValidSources()
    {
        WriteSource("Entities", "GameSession.cs", $$"""
            namespace {{_solutionName}}.Entities;

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
        WriteSource("UseCases", "StartGameCommand.cs", $$"""
            namespace {{_solutionName}}.UseCases;

            public class StartGameCommand
            {
            }
            """);
        WriteSource("UseCases", "StartGameResult.cs", $$"""
            namespace {{_solutionName}}.UseCases;

            public class StartGameResult
            {
                public bool IsSuccess { get; set; }

                public string? FailureCode { get; set; }

                public string? FailureMessage { get; set; }
            }
            """);
        WriteSource("UseCases", "StartGameUseCase.cs", $$"""
            namespace {{_solutionName}}.UseCases;

            public class StartGameUseCase
            {
                public StartGameResult Execute(StartGameCommand command)
                {
                    return new StartGameResult
                    {
                        IsSuccess = true
                    };
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
            using {{_solutionName}}.UseCases;

            namespace {{_solutionName}}.WinForms;

            public class GamePresenter
            {
                private IGameView _view;
                private StartGameUseCase _startGameUseCase;

                public GamePresenter(IGameView view, StartGameUseCase startGameUseCase)
                {
                    _view = view;
                    _startGameUseCase = startGameUseCase;
                }

                public void Start()
                {
                    StartGameResult result = _startGameUseCase.Execute(new StartGameCommand());
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
            using {{_solutionName}}.UseCases;
            using {{_solutionName}}.WinForms;

            ApplicationConfiguration.Initialize();
            IGameView view = new GameForm(null!);
            StartGameUseCase useCase = new StartGameUseCase();
            GamePresenter presenter = new GamePresenter(view, useCase);
            Application.Run((GameForm)view);
            """);
    }
}
