using System.Text.Json;
using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class ProjectVerificationRunnerTests
{
    [Fact]
    public void VerifyWritesManifestAndReportsWithoutBlockingOnAuditFindings()
    {
        string projectRoot = FindProjectRoot();
        string outputPath = Path.Combine(Path.GetTempPath(), $"verify-project-{Guid.NewGuid():N}");
        ProjectVerificationRunner runner = new ProjectVerificationRunner();

        try
        {
            GateRunResult result = runner.Verify(
                Path.Combine(projectRoot, "Pattern Network"),
                Path.Combine(projectRoot, "Domains"),
                Path.Combine(projectRoot, "misfit.yaml"),
                Path.Combine(projectRoot, "Snake.sln"),
                projectRoot,
                outputPath);

            Assert.True(result.IsSuccessful, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
            Assert.Contains(result.Checks, check =>
                check.Mode == VerificationMode.Audit
                && (check.Status == VerificationStatus.Fail
                    || check.Status == VerificationStatus.Pending));
            Assert.DoesNotContain(result.Checks, check =>
                check.CheckId.StartsWith("SNAKE-CODE-", StringComparison.Ordinal)
                && check.Status == VerificationStatus.NotRun);
            string manifestPath = Path.Combine(outputPath, "verification-manifest.generated.yaml");
            string jsonPath = Path.Combine(outputPath, "verification-report.json");
            string markdownPath = Path.Combine(outputPath, "verification-report.md");
            Assert.True(File.Exists(manifestPath));
            Assert.True(File.Exists(jsonPath));
            Assert.True(File.Exists(markdownPath));
            string manifest = File.ReadAllText(manifestPath);
            Assert.Contains("V3-CATALOG-SNAKE-ACTIVE", manifest);
            Assert.Contains("kind: acceptance_contract", manifest);
            using JsonDocument report = JsonDocument.Parse(File.ReadAllText(jsonPath));
            Assert.True(report.RootElement.GetProperty("isSuccessful").GetBoolean());
            Assert.DoesNotContain(
                report.RootElement.GetProperty("checks").EnumerateArray(),
                check => check.GetProperty("checkId").GetString()?.StartsWith(
                    "SNAKE-CODE-",
                    StringComparison.Ordinal) == true
                    && check.GetProperty("status").GetString() == "not_run");
            Assert.Contains("AUDIT", File.ReadAllText(markdownPath));
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
        }
    }

    [Fact]
    public void VerifyTreatsLegacyMisfitShapeAsConfigurationError()
    {
        string projectRoot = FindProjectRoot();
        string temporaryPath = Path.Combine(Path.GetTempPath(), $"verify-project-invalid-{Guid.NewGuid():N}");
        string outputPath = Path.Combine(temporaryPath, "output");
        string misfitPath = Path.Combine(temporaryPath, "misfit.yaml");
        Directory.CreateDirectory(temporaryPath);
        File.WriteAllText(misfitPath, """
            schema_version: 1
            acceptance_scenarios: []
            pattern_gates: []
            """);
        ProjectVerificationRunner runner = new ProjectVerificationRunner();

        try
        {
            GateRunResult result = runner.Verify(
                Path.Combine(projectRoot, "Pattern Network"),
                Path.Combine(projectRoot, "Domains"),
                misfitPath,
                Path.Combine(projectRoot, "Snake.sln"),
                projectRoot,
                outputPath);

            Assert.True(result.HasConfigurationError);
            Assert.False(result.IsSuccessful);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-MISFIT-SCHEMA");
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-MISFIT-UNKNOWN-FIELD");
        }
        finally
        {
            Directory.Delete(temporaryPath, true);
        }
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
}
