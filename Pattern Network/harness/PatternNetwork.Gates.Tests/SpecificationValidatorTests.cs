using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class SpecificationValidatorTests
{
    [Fact]
    public void ValidateAcceptsDomainContractAndTechnicalDomainReference()
    {
        string domainsPath = CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["frame.yaml"] = """
                schema_version: 3
                spec_kind: problem_frame
                id: V3-FRAME-GAME-MOVE-SNAKE
                frame: {}
                """,
            ["domain_spec.yaml"] = """
                schema_version: 3
                spec_kind: domain_contract
                id: V3-DOMAIN-GAME-MOVE-SNAKE
                domain: MoveSnake
                frame_ref: V3-FRAME-GAME-MOVE-SNAKE
                use_case: MoveSnake
                input: {}
                output: {}
                rules: []
                """,
            ["technical_spec.yaml"] = """
                schema_version: 3
                spec_kind: technical_contract
                id: V3-TECH-GAME-MOVE-SNAKE
                operation: MoveSnake
                domain_ref: V3-DOMAIN-GAME-MOVE-SNAKE
                dependency_injection: {}
                dto: {}
                """
        });

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(domainsPath);

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
            Assert.Contains(
                result.Inventory.Documents,
                document => document.Header.SpecificationKind == SpecificationKind.DomainContract);
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateAcceptsAValidV3ProblemFrame()
    {
        string domainsPath = CreateTemporaryDomains("""
            schema_version: 3
            spec_kind: problem_frame
            id: V3-FRAME-GAME-START-CURRENT-LEVEL
            frame:
              bounded_context: snake-game
              frame_type: CBF
              requirement: "玩家開始目前關卡。"
            """);

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(domainsPath);

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
            Assert.Single(result.Inventory.Documents);
            Assert.Equal(
                SpecificationKind.ProblemFrame,
                result.Inventory.Documents[0].Header.SpecificationKind);
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsV1Documents()
    {
        string domainsPath = CreateTemporaryDomains("""
            schema_version: 1
            spec_kind: problem_frame
            id: V3-FRAME-GAME-START-CURRENT-LEVEL
            frame: {}
            """);

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(domainsPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-SPEC-SCHEMA-VERSION");
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsUnknownTopLevelFields()
    {
        string domainsPath = CreateTemporaryDomains("""
            schema_version: 3
            spec_kind: problem_frame
            id: V3-FRAME-GAME-START-CURRENT-LEVEL
            frame: {}
            required_behavior: "不應出現在 frame。"
            """);

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(domainsPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue =>
                issue.RuleId == "V3-SPEC-UNKNOWN-FIELD"
                && issue.Message.Contains("required_behavior", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsMissingWorkflowReferences()
    {
        string domainsPath = CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["technical_spec.yaml"] = """
                schema_version: 3
                spec_kind: technical_contract
                id: V3-TECH-GAME-START-CURRENT-LEVEL
                operation: StartCurrentLevel
                workflow_ref: V3-WF-GAME-START-CURRENT-LEVEL
                """
        });

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(domainsPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-SPEC-REFERENCE-MISSING");
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsReferencesToTheWrongSpecificationKind()
    {
        string domainsPath = CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["snake.yaml"] = """
                schema_version: 3
                spec_kind: aggregate_contract
                id: V3-AGG-SNAKE-SNAKE
                aggregate: Snake
                """,
            ["technical_spec.yaml"] = """
                schema_version: 3
                spec_kind: technical_contract
                id: V3-TECH-GAME-START-CURRENT-LEVEL
                operation: StartCurrentLevel
                workflow_ref: V3-AGG-SNAKE-SNAKE
                """
        });

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(domainsPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-SPEC-REFERENCE-KIND");
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateEntriesLoadsReferenceClosureAndIgnoresUnrelatedV1Documents()
    {
        string domainsPath = CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["legacy.yaml"] = """
                schema_version: 1
                frame_id: LEGACY-FRAME-001
                """,
            ["frame.yaml"] = """
                schema_version: 3
                spec_kind: problem_frame
                id: V3-FRAME-GAME-START-CURRENT-LEVEL
                frame: {}
                """,
            ["workflow.yaml"] = """
                schema_version: 3
                spec_kind: workflow_contract
                id: V3-WF-GAME-START-CURRENT-LEVEL
                workflow: StartCurrentLevel
                frame_ref: V3-FRAME-GAME-START-CURRENT-LEVEL
                """,
            ["technical_spec.yaml"] = """
                schema_version: 3
                spec_kind: technical_contract
                id: V3-TECH-GAME-START-CURRENT-LEVEL
                operation: StartCurrentLevel
                workflow_ref: V3-WF-GAME-START-CURRENT-LEVEL
                """
        });

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(
                domainsPath,
                new List<string> { "technical_spec.yaml" });

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
            Assert.Equal(3, result.Inventory.Documents.Count);
            Assert.DoesNotContain(result.Inventory.Documents, document => document.FilePath.EndsWith("legacy.yaml", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateEntriesRejectsAnEntryOutsideTheDomainsDirectory()
    {
        string domainsPath = CreateTemporaryDomains("""
            schema_version: 3
            spec_kind: problem_frame
            id: V3-FRAME-GAME-START-CURRENT-LEVEL
            frame: {}
            """);
        string outsidePath = Path.Combine(Path.GetTempPath(), $"outside-specification-{Guid.NewGuid():N}.yaml");
        File.WriteAllText(outsidePath, """
            schema_version: 3
            spec_kind: problem_frame
            id: V3-FRAME-GAME-OUTSIDE
            frame: {}
            """);

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(
                domainsPath,
                new List<string> { outsidePath });

            Assert.False(result.IsValid);
            Assert.True(result.HasConfigurationError);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-SPEC-CONFIG-ENTRY");
        }
        finally
        {
            Directory.Delete(domainsPath, true);
            File.Delete(outsidePath);
        }
    }

    [Fact]
    public void ValidateRejectsIncorrectTopLevelFieldTypes()
    {
        string domainsPath = CreateTemporaryDomains("""
            schema_version: 3
            spec_kind: problem_frame
            id: V3-FRAME-GAME-START-CURRENT-LEVEL
            frame: StartCurrentLevel
            """);

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(domainsPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue =>
                issue.RuleId == "V3-SPEC-FIELD-TYPE"
                && issue.Message.Contains("frame", StringComparison.Ordinal)
                && issue.Message.Contains("mapping", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateEntriesLoadsWorkflowDomainReferences()
    {
        string domainsPath = CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["snake.yaml"] = """
                schema_version: 3
                spec_kind: aggregate_contract
                id: V3-AGG-SNAKE-SNAKE
                aggregate: Snake
                """,
            ["frame.yaml"] = """
                schema_version: 3
                spec_kind: problem_frame
                id: V3-FRAME-GAME-PROCESS-TICK
                frame: {}
                """,
            ["workflow.yaml"] = """
                schema_version: 3
                spec_kind: workflow_contract
                id: V3-WF-GAME-PROCESS-TICK
                workflow: ProcessGameTick
                frame_ref: V3-FRAME-GAME-PROCESS-TICK
                domain_refs:
                  - V3-AGG-SNAKE-SNAKE
                """
        });

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(
                domainsPath,
                new List<string> { "workflow.yaml" });

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
            Assert.Equal(3, result.Inventory.Documents.Count);
            Assert.Contains(
                result.Inventory.Documents,
                document => document.Header.SpecificationKind == SpecificationKind.AggregateContract);
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateEntriesLoadsReferencedTechnicalContract()
    {
        string domainsPath = CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["frame.yaml"] = """
                schema_version: 3
                spec_kind: problem_frame
                id: V3-FRAME-GAME-GET-CURRENT-SCREEN
                frame: {}
                """,
            ["workflow.yaml"] = """
                schema_version: 3
                spec_kind: workflow_contract
                id: V3-WF-GAME-GET-CURRENT-SCREEN
                workflow: GetCurrentScreen
                frame_ref: V3-FRAME-GAME-GET-CURRENT-SCREEN
                """,
            ["screen-technical.yaml"] = """
                schema_version: 3
                spec_kind: technical_contract
                id: V3-TECH-GAME-GET-CURRENT-SCREEN
                operation: GetCurrentScreen
                workflow_ref: V3-WF-GAME-GET-CURRENT-SCREEN
                """,
            ["tick-technical.yaml"] = """
                schema_version: 3
                spec_kind: technical_contract
                id: V3-TECH-GAME-PROCESS-TICK
                operation: ProcessGameTick
                workflow_ref: V3-WF-GAME-GET-CURRENT-SCREEN
                technical_ref: V3-TECH-GAME-GET-CURRENT-SCREEN
                """
        });

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(
                domainsPath,
                new List<string> { "tick-technical.yaml" });

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
            Assert.Equal(4, result.Inventory.Documents.Count);
            Assert.Contains(
                result.Inventory.Documents,
                document => document.Header.Id == "V3-TECH-GAME-GET-CURRENT-SCREEN");
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateEntriesLoadsJourneyPresentationStateReferences()
    {
        string domainsPath = CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["presentation-state.yaml"] = """
                schema_version: 3
                spec_kind: presentation_state
                id: V3-PRES-STATE-GAME-FIRST-LEVEL-READY
                screen: FirstLevelReady
                """,
            ["journey.yaml"] = """
                schema_version: 3
                spec_kind: journey_contract
                id: V3-JOURNEY-GAME-PLAY
                journey: PlayGame
                state_refs:
                  - V3-PRES-STATE-GAME-FIRST-LEVEL-READY
                """
        });

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(
                domainsPath,
                new List<string> { "journey.yaml" });

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
            Assert.Equal(2, result.Inventory.Documents.Count);
            Assert.Contains(
                result.Inventory.Documents,
                document => document.Header.Id == "V3-PRES-STATE-GAME-FIRST-LEVEL-READY");
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateEntriesLoadsPresentationWorkflowApplicationWorkflowReference()
    {
        string domainsPath = CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["application-workflow.yaml"] = """
                schema_version: 3
                spec_kind: workflow_contract
                id: V3-WF-GAME-SET-SNAKE-DIRECTION
                workflow: SetSnakeDirection
                """,
            ["presentation-workflow.yaml"] = """
                schema_version: 3
                spec_kind: presentation_workflow
                id: V3-PRES-WF-GAME-HANDLE-DIRECTION-INPUT
                workflow: HandleDirectionInput
                workflow_ref: V3-WF-GAME-SET-SNAKE-DIRECTION
                """
        });

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(
                domainsPath,
                new List<string> { "presentation-workflow.yaml" });

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
            Assert.Equal(2, result.Inventory.Documents.Count);
            Assert.Contains(
                result.Inventory.Documents,
                document => document.Header.Id == "V3-WF-GAME-SET-SNAKE-DIRECTION");
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateEntriesLoadsTypedCatalogReferences()
    {
        string domainsPath = CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["frame.yaml"] = """
                schema_version: 3
                spec_kind: problem_frame
                id: V3-FRAME-GAME-PROCESS-TICK
                frame: {}
                """,
            ["aggregate.yaml"] = """
                schema_version: 3
                spec_kind: aggregate_contract
                id: V3-AGG-SNAKE-SNAKE
                aggregate: Snake
                """,
            ["domain.yaml"] = """
                schema_version: 3
                spec_kind: domain_contract
                id: V3-DOMAIN-GAME-MOVE-SNAKE
                domain: MoveSnake
                """,
            ["workflow.yaml"] = """
                schema_version: 3
                spec_kind: workflow_contract
                id: V3-WF-GAME-PROCESS-TICK
                workflow: ProcessGameTick
                frame_ref: V3-FRAME-GAME-PROCESS-TICK
                """,
            ["technical.yaml"] = """
                schema_version: 3
                spec_kind: technical_contract
                id: V3-TECH-GAME-PROCESS-TICK
                operation: ProcessGameTick
                workflow_ref: V3-WF-GAME-PROCESS-TICK
                """,
            ["acceptance.yaml"] = """
                schema_version: 3
                spec_kind: acceptance_contract
                id: V3-ACCEPT-GAME-PROCESS-TICK
                contract_ref: V3-WF-GAME-PROCESS-TICK
                scenarios: []
                """,
            ["journey.yaml"] = """
                schema_version: 3
                spec_kind: journey_contract
                id: V3-JOURNEY-GAME-PLAY
                journey: PlayGame
                """,
            ["presentation-state.yaml"] = """
                schema_version: 3
                spec_kind: presentation_state
                id: V3-PRES-STATE-GAME-PLAYING
                screen: GamePlaying
                """,
            ["presentation-workflow.yaml"] = """
                schema_version: 3
                spec_kind: presentation_workflow
                id: V3-PRES-WF-GAME-RENDER
                workflow: RenderGameScreen
                state_refs:
                  - V3-PRES-STATE-GAME-PLAYING
                """,
            ["migration.yaml"] = """
                schema_version: 3
                spec_kind: migration_map
                id: V3-MIGRATION-GAME-V1-V3
                migration: GameV1ToV3
                entries: []
                """,
            ["catalog.yaml"] = """
                schema_version: 3
                spec_kind: spec_catalog
                id: V3-CATALOG-GAME-SPECIFICATIONS
                catalog: GameSpecifications
                frames:
                  - V3-FRAME-GAME-PROCESS-TICK
                aggregates:
                  - V3-AGG-SNAKE-SNAKE
                domain_contracts:
                  - V3-DOMAIN-GAME-MOVE-SNAKE
                workflows:
                  - V3-WF-GAME-PROCESS-TICK
                technical_contracts:
                  - V3-TECH-GAME-PROCESS-TICK
                acceptance_contracts:
                  - V3-ACCEPT-GAME-PROCESS-TICK
                journeys:
                  - V3-JOURNEY-GAME-PLAY
                presentation_states:
                  - V3-PRES-STATE-GAME-PLAYING
                presentation_workflows:
                  - V3-PRES-WF-GAME-RENDER
                migrations:
                  - V3-MIGRATION-GAME-V1-V3
                """
        });

        try
        {
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(
                domainsPath,
                new List<string> { "catalog.yaml" });

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
            Assert.Equal(11, result.Inventory.Documents.Count);
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateEntriesNormalizesRelativePathsAcrossCircularReferences()
    {
        string domainsPath = CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["workflow.yaml"] = """
                schema_version: 3
                spec_kind: presentation_workflow
                id: V3-PRES-WF-GAME-RENDER-SCREEN
                workflow: RenderGameScreen
                acceptance_ref: V3-ACCEPT-GAME-RENDER-SCREEN
                """,
            ["acceptance.yaml"] = """
                schema_version: 3
                spec_kind: acceptance_contract
                id: V3-ACCEPT-GAME-RENDER-SCREEN
                contract_ref: V3-PRES-WF-GAME-RENDER-SCREEN
                scenarios: []
                """
        });

        try
        {
            string relativeDomainsPath = Path.GetRelativePath(
                Directory.GetCurrentDirectory(),
                domainsPath);
            SpecificationValidator validator = new SpecificationValidator();

            SpecificationValidationResult result = validator.Validate(
                relativeDomainsPath,
                new List<string> { "workflow.yaml" });

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
            Assert.Equal(2, result.Inventory.Documents.Count);
            Assert.Equal(
                2,
                result.Inventory.Documents
                    .Select(document => document.Header.Id)
                    .Distinct(StringComparer.Ordinal)
                    .Count());
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    private string CreateTemporaryDomains(string specification)
    {
        return CreateTemporaryDomains(new Dictionary<string, string>
        {
            ["frame.yaml"] = specification
        });
    }

    private string CreateTemporaryDomains(Dictionary<string, string> specifications)
    {
        string domainsPath = Path.Combine(Path.GetTempPath(), $"v3-specifications-{Guid.NewGuid():N}");
        Directory.CreateDirectory(domainsPath);
        foreach (KeyValuePair<string, string> specification in specifications)
        {
            File.WriteAllText(Path.Combine(domainsPath, specification.Key), specification.Value);
        }

        return domainsPath;
    }
}
