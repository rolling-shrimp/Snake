using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class PresentationContractValidatorTests
{
    [Fact]
    public void ValidateAcceptsRepositoryPresentationContracts()
    {
        string projectRoot = FindProjectRoot();
        PresentationContractValidator validator = new PresentationContractValidator();

        PresentationContractValidationResult result = validator.Validate(
            Path.Combine(projectRoot, "Domains"),
            projectRoot);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
    }

    [Fact]
    public void ValidateRejectsCatalogWithoutAllPresentationStates()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
        try
        {
            string catalogPath = Path.Combine(temporaryProjectRoot, "Domains", "catalog.yaml");
            RemoveLineContaining(catalogPath, "V3-PRES-STATE-GAME-GAME-OVER");
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-CATALOG-STATES");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsVisibilityOverlap()
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
                "    - game-board",
                "    - game-over-message",
                StringComparison.Ordinal));
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-VISIBILITY-CONFLICT");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsUnknownTransitionTargetState()
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
                "V3-PRES-STATE-GAME-FIRST-LEVEL-PLAYING",
                "V3-PRES-STATE-GAME-UNKNOWN",
                StringComparison.Ordinal));
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-TRANSITION-TARGET");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsRenderWorkflowWithoutAllStates()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
        try
        {
            string workflowPath = Path.Combine(
                temporaryProjectRoot,
                "Domains",
                "Game",
                "presentation",
                "workflows",
                "render-game-screen",
                "workflow.yaml");
            RemoveLineContaining(workflowPath, "V3-PRES-STATE-GAME-GAME-COMPLETED");
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-RENDER-STATES");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsRenderWorkflowWithWrongOutputGrain()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
        try
        {
            string workflowPath = Path.Combine(
                temporaryProjectRoot,
                "Domains",
                "Game",
                "presentation",
                "workflows",
                "render-game-screen",
                "workflow.yaml");
            string workflow = File.ReadAllText(workflowPath);
            File.WriteAllText(workflowPath, workflow.Replace(
                "changed_ui_controls_and_board_cells",
                "one_complete_rendered_screen",
                StringComparison.Ordinal));
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-RENDER-GRAIN");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsRenderWorkflowThatRecreatesControls()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
        try
        {
            string workflowPath = Path.Combine(
                temporaryProjectRoot,
                "Domains",
                "Game",
                "presentation",
                "workflows",
                "render-game-screen",
                "workflow.yaml");
            string workflow = File.ReadAllText(workflowPath);
            File.WriteAllText(workflowPath, workflow.Replace(
                "create_once_then_update_changed_cells",
                "recreate_all_controls",
                StringComparison.Ordinal));
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-RENDER-POLICY");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsDirectionInputWorkflowAfterControlNavigation()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
        try
        {
            string workflowPath = Path.Combine(
                temporaryProjectRoot,
                "Domains",
                "Game",
                "presentation",
                "workflows",
                "handle-direction-input",
                "workflow.yaml");
            string workflow = File.ReadAllText(workflowPath);
            File.WriteAllText(workflowPath, workflow.Replace(
                "ProcessCmdKey",
                "key_down_after_control_navigation",
                StringComparison.Ordinal));
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-DIRECTION-DELIVERY");
        }
        finally
        {
            Directory.Delete(temporaryProjectRoot, true);
        }
    }

    [Fact]
    public void ValidateRejectsDirectionInputWorkflowWithIncompleteMapping()
    {
        string temporaryProjectRoot = CreateTemporaryProject();
        try
        {
            string workflowPath = Path.Combine(
                temporaryProjectRoot,
                "Domains",
                "Game",
                "presentation",
                "workflows",
                "handle-direction-input",
                "workflow.yaml");
            RemoveLineContaining(workflowPath, "Right: { x: 1, y: 0 }");
            PresentationContractValidator validator = new PresentationContractValidator();

            PresentationContractValidationResult result = validator.Validate(
                Path.Combine(temporaryProjectRoot, "Domains"),
                temporaryProjectRoot);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "PRES-DIRECTION-MAPPING");
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
                $"{Environment.NewLine}presentation_refs:{Environment.NewLine}  - V3-PRES-STATE-GAME-GAME-OVER{Environment.NewLine}");
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

    private void RemoveLineContaining(string path, string value)
    {
        List<string> lines = File.ReadAllLines(path)
            .Where(line => !line.Contains(value, StringComparison.Ordinal))
            .ToList();
        File.WriteAllLines(path, lines);
    }
}
