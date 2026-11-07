using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class PatternNetworkValidatorTests
{
    [Fact]
    public void ValidateAcceptsTheRepositoryPatternNetwork()
    {
        string networkPath = FindPatternNetworkPath();
        PatternNetworkValidator validator = new PatternNetworkValidator();

        PatternNetworkValidationResult result = validator.Validate(networkPath);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
        Assert.Equal(new[] { "Application", "Domain", "Infrastructure", "WinForms" }, result.Definition.PatternNodes.Select(node => node.Name));
        Assert.Equal("Test", result.Definition.VerificationProject.SolutionFolder);
        Assert.Equal("Test", result.Definition.VerificationProject.Name);
    }

    [Fact]
    public void ValidateRejectsUnknownRuleReferences()
    {
        string sourcePath = FindPatternNetworkPath();
        string temporaryPath = Path.Combine(Path.GetTempPath(), $"pattern-network-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryPath);

        try
        {
            CopyDirectory(sourcePath, temporaryPath);
            string gatePath = Path.Combine(temporaryPath, "patterns", "domain", "gate.yaml");
            string gateText = File.ReadAllText(gatePath);
            File.WriteAllText(gatePath, gateText.Replace("V3-PN-NAME-001", "V3-PN-UNKNOWN-001", StringComparison.Ordinal));
            PatternNetworkValidator validator = new PatternNetworkValidator();

            PatternNetworkValidationResult result = validator.Validate(temporaryPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-CONFIG-RULE-REF");
        }
        finally
        {
            Directory.Delete(temporaryPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsGateParametersThatDriftFromDoctrine()
    {
        string sourcePath = FindPatternNetworkPath();
        string temporaryPath = Path.Combine(Path.GetTempPath(), $"pattern-network-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryPath);

        try
        {
            CopyDirectory(sourcePath, temporaryPath);
            string gatePath = Path.Combine(temporaryPath, "patterns", "domain", "gate.yaml");
            string gateText = File.ReadAllText(gatePath);
            File.WriteAllText(gatePath, gateText.Replace("target_framework: net8.0", "target_framework: net9.0", StringComparison.Ordinal));
            PatternNetworkValidator validator = new PatternNetworkValidator();

            PatternNetworkValidationResult result = validator.Validate(temporaryPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-CONFIG-GATE-DRIFT");
        }
        finally
        {
            Directory.Delete(temporaryPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsSchemaVersionOnePatternNetworks()
    {
        string sourcePath = FindPatternNetworkPath();
        string temporaryPath = Path.Combine(Path.GetTempPath(), $"pattern-network-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryPath);

        try
        {
            CopyDirectory(sourcePath, temporaryPath);
            string doctrinePath = Path.Combine(temporaryPath, "doctrine.yaml");
            string doctrineText = File.ReadAllText(doctrinePath);
            File.WriteAllText(doctrinePath, doctrineText.Replace(
                "schema_version: 3",
                "schema_version: 1",
                StringComparison.Ordinal));
            PatternNetworkValidator validator = new PatternNetworkValidator();

            PatternNetworkValidationResult result = validator.Validate(temporaryPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-PN-CONFIG-LOAD");
        }
        finally
        {
            Directory.Delete(temporaryPath, true);
        }
    }

    [Fact]
    public void ValidateLoadsUseCaseAndUiRoleChecksAsBlocking()
    {
        PatternNetworkValidator validator = new PatternNetworkValidator();

        PatternNetworkValidationResult result = validator.Validate(FindPatternNetworkPath());

        PatternNodeDefinition application = Assert.Single(
            result.Definition.PatternNodes,
            node => node.Id == "application");
        GateCheckDefinition useCaseCheck = Assert.Single(
            application.Checks,
            check => check.Checker == "application_use_case_contracts");
        PatternNodeDefinition winForms = Assert.Single(
            result.Definition.PatternNodes,
            node => node.Id == "winforms");
        Assert.Contains(winForms.Checks, check => check.Checker == "winforms_roles");
        Assert.Contains(winForms.Checks, check => check.Checker == "game_form_boundaries");
        Assert.Contains(winForms.Checks, check => check.Checker == "winforms_program");
        PatternNodeDefinition infrastructure = Assert.Single(
            result.Definition.PatternNodes,
            node => node.Id == "infrastructure");
        GateCheckDefinition infrastructureDependencyInjection = Assert.Single(
            infrastructure.Checks,
            check => check.Checker == "dependency_injection");
        Assert.Equal("AddInfrastructure", infrastructureDependencyInjection.ScalarParameters["registration_method"]);
        Assert.Equal(VerificationMode.Blocking, useCaseCheck.Mode);
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

    private void CopyDirectory(string sourcePath, string destinationPath)
    {
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
