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
        Assert.Equal(new[] { "Entities", "UseCases", "WinForms" }, result.Definition.PatternNodes.Select(node => node.Name));
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
            string gatePath = Path.Combine(temporaryPath, "patterns", "entities", "gate.yaml");
            string gateText = File.ReadAllText(gatePath);
            File.WriteAllText(gatePath, gateText.Replace("PN-NAME-001", "PN-UNKNOWN-001", StringComparison.Ordinal));
            PatternNetworkValidator validator = new PatternNetworkValidator();

            PatternNetworkValidationResult result = validator.Validate(temporaryPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PN-CONFIG-RULE-REF");
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
            string gatePath = Path.Combine(temporaryPath, "patterns", "entities", "gate.yaml");
            string gateText = File.ReadAllText(gatePath);
            File.WriteAllText(gatePath, gateText.Replace("target_framework: net8.0", "target_framework: net9.0", StringComparison.Ordinal));
            PatternNetworkValidator validator = new PatternNetworkValidator();

            PatternNetworkValidationResult result = validator.Validate(temporaryPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PN-CONFIG-GATE-DRIFT");
        }
        finally
        {
            Directory.Delete(temporaryPath, true);
        }
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
