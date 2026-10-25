using System.Text.Json;
using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class GateApplicationTests
{
    [Fact]
    public void ValidateNetworkReturnsSuccessForTheRepositoryRules()
    {
        GateApplication application = new GateApplication();
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();

        int exitCode = application.Run(new[] { "validate-network", "--network", FindPatternNetworkPath() }, output, error);

        Assert.Equal(0, exitCode);
        Assert.Contains("PASS", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void ValidatePresentationsReturnsSuccessForRepositoryContracts()
    {
        string projectRoot = FindProjectRoot();
        GateApplication application = new GateApplication();
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();

        int exitCode = application.Run(
            new[]
            {
                "validate-presentations",
                "--domains",
                Path.Combine(projectRoot, "Domains"),
                "--project-root",
                projectRoot
            },
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("PASS", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void ValidatePresentationsReturnsViolationExitCodeForBrokenReference()
    {
        string temporaryProjectRoot = CreateTemporaryPresentationProject();
        try
        {
            string contractPath = Path.Combine(
                temporaryProjectRoot,
                "Domains",
                "Game",
                "presentation",
                "game-over",
                "presentation.yaml");
            string contract = File.ReadAllText(contractPath);
            File.WriteAllText(contractPath, contract.Replace(
                "IDF-SHOW-GAME-OVER-RESULT-001",
                "IDF-UNKNOWN-001",
                StringComparison.Ordinal));
            GateApplication application = new GateApplication();
            StringWriter output = new StringWriter();
            StringWriter error = new StringWriter();

            int exitCode = application.Run(
                new[]
                {
                    "validate-presentations",
                    "--domains",
                    Path.Combine(temporaryProjectRoot, "Domains"),
                    "--project-root",
                    temporaryProjectRoot
                },
                output,
                error);

            Assert.Equal(1, exitCode);
            Assert.Contains("PRES-FRAME-REF", output.ToString());
            Assert.Equal(string.Empty, error.ToString());
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidatePresentationsReturnsConfigurationExitCodeWhenDomainsAreMissing()
    {
        string projectRoot = FindProjectRoot();
        GateApplication application = new GateApplication();
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();

        int exitCode = application.Run(
            new[]
            {
                "validate-presentations",
                "--domains",
                Path.Combine(projectRoot, "missing-domains"),
                "--project-root",
                projectRoot
            },
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("PRES-CONFIG-DOMAINS", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public void CheckCodeWritesTextAndJsonResults()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        string jsonPath = Path.Combine(Path.GetTempPath(), $"pattern-network-report-{Guid.NewGuid():N}.json");
        GateApplication application = new GateApplication();
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();

        try
        {
            int exitCode = application.Run(
                new[]
                {
                    "check-code",
                    "--network",
                    FindPatternNetworkPath(),
                    "--solution",
                    solution.SolutionPath,
                    "--json-output",
                    jsonPath
                },
                output,
                error);

            Assert.Equal(0, exitCode);
            Assert.Contains("PASS", output.ToString());
            using JsonDocument report = JsonDocument.Parse(File.ReadAllText(jsonPath));
            Assert.True(report.RootElement.GetProperty("isSuccessful").GetBoolean());
            Assert.Equal(0, report.RootElement.GetProperty("issues").GetArrayLength());
        }
        finally
        {
            if (File.Exists(jsonPath))
            {
                File.Delete(jsonPath);
            }
        }
    }

    [Fact]
    public void InvalidCommandReturnsConfigurationExitCode()
    {
        GateApplication application = new GateApplication();
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();

        int exitCode = application.Run(new[] { "unknown-command" }, output, error);

        Assert.Equal(2, exitCode);
        Assert.Contains("validate-network", error.ToString());
    }

    [Fact]
    public void CheckCodeReturnsViolationExitCodeForArchitectureViolation()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        solution.SetProjectReferences("WinForms", new List<string> { "UseCases", "Entities" });
        GateApplication application = new GateApplication();
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();

        int exitCode = application.Run(
            new[] { "check-code", "--network", FindPatternNetworkPath(), "--solution", solution.SolutionPath },
            output,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains("PN-ARCH-001", output.ToString());
    }

    [Fact]
    public void CheckCodeReturnsConfigurationExitCodeWhenTargetProjectIsMissing()
    {
        using TemporarySolution solution = new TemporarySolution("SampleGame");
        Directory.Delete(Path.Combine(Path.GetDirectoryName(solution.SolutionPath)!, "SampleGame.Entities"), true);
        GateApplication application = new GateApplication();
        StringWriter output = new StringWriter();
        StringWriter error = new StringWriter();

        int exitCode = application.Run(
            new[] { "check-code", "--network", FindPatternNetworkPath(), "--solution", solution.SolutionPath },
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("PN-TARGET-NOT-FOUND", output.ToString());
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

    private string FindProjectRoot()
    {
        DirectoryInfo? currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory != null)
        {
            string candidate = Path.Combine(currentDirectory.FullName, "Snake.sln");
            if (File.Exists(candidate))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("找不到 Snake.sln。");
    }

    private string CreateTemporaryPresentationProject()
    {
        string sourceProjectRoot = FindProjectRoot();
        string temporaryProjectRoot = Path.Combine(Path.GetTempPath(), $"presentation-command-{Guid.NewGuid():N}");
        CopyDirectory(Path.Combine(sourceProjectRoot, "Domains"), Path.Combine(temporaryProjectRoot, "Domains"));
        string assetsPath = Path.Combine(temporaryProjectRoot, "assets");
        Directory.CreateDirectory(assetsPath);
        foreach (string extension in new[] { "html", "png" })
        {
            for (int level = 1; level <= 3; level++)
            {
                string fileName = $"level-{level}.{extension}";
                File.Copy(Path.Combine(sourceProjectRoot, "assets", fileName), Path.Combine(assetsPath, fileName));
            }
        }

        return temporaryProjectRoot;
    }

    private void CopyDirectory(string sourcePath, string destinationPath)
    {
        Directory.CreateDirectory(destinationPath);
        foreach (string directoryPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directoryPath.Replace(sourcePath, destinationPath, StringComparison.Ordinal));
        }

        foreach (string filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            string destinationFilePath = filePath.Replace(sourcePath, destinationPath, StringComparison.Ordinal);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath)!);
            File.Copy(filePath, destinationFilePath);
        }
    }
}
