using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class PresentationContractValidatorTests
{
    [Fact]
    public void ValidateAcceptsRepositoryPresentationContracts()
    {
        string projectRoot = FindProjectRoot();
        string domainsPath = Path.Combine(projectRoot, "Domains");
        PresentationContractValidator validator = new PresentationContractValidator();

        PresentationContractValidationResult result = validator.Validate(domainsPath, projectRoot);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
    }

    [Fact]
    public void ValidateRejectsDuplicateRelationIdsAcrossPresentation()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
        try
        {
            string contractPath = Path.Combine(
                temporaryProjectRoot,
                "Domains",
                "Game",
                "presentation",
                "first-level-ready",
                "presentation.yaml");
            string contract = File.ReadAllText(contractPath);
            File.WriteAllText(contractPath, contract.Replace(
                "PRES-FIRST-READY-BOARD",
                "PRES-FIRST-READY-RULES",
                StringComparison.Ordinal));
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-RELATION-DUPLICATE");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsUnknownFrameCheckReference()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
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
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-FRAME-REF");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsTransitionDrift()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
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
                "target_screen: first-level-playing",
                "target_screen: first-level-ready",
                StringComparison.Ordinal));
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-TRANSITION-MISSING");
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-TRANSITION-UNEXPECTED");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsTransitionLinkedToWrongUseCase()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
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
                "relation_ref: PRES-GAME-OVER-REPLAY",
                "relation_ref: PRES-GAME-OVER-RESULT",
                StringComparison.Ordinal));
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-TRANSITION-USE-CASE");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsNonReferenceOnlyOwnership()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
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
                "ownership: reference_only",
                "ownership: aggregate",
                StringComparison.Ordinal));
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-SCHEMA-VALUE");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsWrongPreviewDimensions()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
        try
        {
            string pngPath = Path.Combine(temporaryProjectRoot, "assets", "level-1.png");
            byte[] png = File.ReadAllBytes(pngPath);
            png[16] = 0;
            png[17] = 0;
            png[18] = 3;
            png[19] = 32;
            File.WriteAllBytes(pngPath, png);
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-ASSET-DIMENSIONS");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsReversePresentationReference()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
        try
        {
            string domainPath = Path.Combine(temporaryProjectRoot, "Domains", "Game", "domain_spec.yaml");
            File.AppendAllText(
                domainPath,
                $"{Environment.NewLine}presentation_refs:{Environment.NewLine}  - presentation/game-over/presentation.yaml{Environment.NewLine}");
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-REVERSE-RELATION");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
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

    private string CreateTemporaryProject()
    {
        string sourceProjectRoot = FindProjectRoot();
        string temporaryProjectRoot = Path.Combine(Path.GetTempPath(), $"presentation-contract-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporaryProjectRoot);
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
