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
}
