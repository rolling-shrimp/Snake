using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class MigrationMapValidatorTests
{
    [Fact]
    public void ValidateAcceptsCompleteLegacySourceAndIdentifierCoverage()
    {
        string projectPath = CreateTemporaryMigrationProject(CreateValidDomainMap());

        try
        {
            MigrationMapValidator validator = new MigrationMapValidator();

            MigrationMapValidationResult result = validator.Validate(
                Path.Combine(projectPath, "Domains"),
                Path.Combine(projectPath, "Pattern Network"));

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Issues.Select(issue => issue.Message)));
        }
        finally
        {
            Directory.Delete(projectPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsLegacySourceWithoutDisposition()
    {
        string projectPath = CreateTemporaryMigrationProject("""
            schema_version: 3
            spec_kind: migration_map
            id: V3-MIGRATION-DOMAINS-V1-V3
            migration: domains-v1-to-v3
            entries: []
            """);

        try
        {
            MigrationMapValidator validator = new MigrationMapValidator();

            MigrationMapValidationResult result = validator.Validate(
                Path.Combine(projectPath, "Domains"),
                Path.Combine(projectPath, "Pattern Network"));

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-MIGRATION-LEGACY-SOURCE-MISSING");
        }
        finally
        {
            Directory.Delete(projectPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsDuplicateLegacyIdentifiers()
    {
        string projectPath = CreateTemporaryMigrationProject("""
            schema_version: 3
            spec_kind: migration_map
            id: V3-MIGRATION-DOMAINS-V1-V3
            migration: domains-v1-to-v3
            entries:
              - source_path: legacy.yaml
                disposition: merged_into
                target_id: V3-AGG-GAME-GAME
                legacy_ids:
                  - old-behavior
                  - OLD-RULE-001
              - source_path: historical.yaml
                disposition: replaced_by
                target_id: V3-AGG-GAME-GAME
                legacy_ids:
                  - OLD-RULE-001
            """);

        try
        {
            MigrationMapValidator validator = new MigrationMapValidator();

            MigrationMapValidationResult result = validator.Validate(
                Path.Combine(projectPath, "Domains"),
                Path.Combine(projectPath, "Pattern Network"));

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-MIGRATION-LEGACY-ID-DUPLICATE");
        }
        finally
        {
            Directory.Delete(projectPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsTargetThatDoesNotExist()
    {
        string projectPath = CreateTemporaryMigrationProject(CreateValidDomainMap().Replace(
            "V3-AGG-GAME-GAME",
            "V3-AGG-GAME-UNKNOWN",
            StringComparison.Ordinal));

        try
        {
            MigrationMapValidator validator = new MigrationMapValidator();

            MigrationMapValidationResult result = validator.Validate(
                Path.Combine(projectPath, "Domains"),
                Path.Combine(projectPath, "Pattern Network"));

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-MIGRATION-TARGET-MISSING");
        }
        finally
        {
            Directory.Delete(projectPath, true);
        }
    }

    [Fact]
    public void ValidateRequiresRetiredEntriesToHaveAReasonAndNoTarget()
    {
        string projectPath = CreateTemporaryMigrationProject("""
            schema_version: 3
            spec_kind: migration_map
            id: V3-MIGRATION-DOMAINS-V1-V3
            migration: domains-v1-to-v3
            entries:
              - source_path: legacy.yaml
                disposition: retired
                target_id: V3-AGG-GAME-GAME
                legacy_ids:
                  - old-behavior
                  - OLD-RULE-001
            """);

        try
        {
            MigrationMapValidator validator = new MigrationMapValidator();

            MigrationMapValidationResult result = validator.Validate(
                Path.Combine(projectPath, "Domains"),
                Path.Combine(projectPath, "Pattern Network"));

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-MIGRATION-RETIRED-REASON");
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-MIGRATION-TARGET-FORM");
        }
        finally
        {
            Directory.Delete(projectPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsSourceMappedByMoreThanOneEntry()
    {
        string projectPath = CreateTemporaryMigrationProject("""
            schema_version: 3
            spec_kind: migration_map
            id: V3-MIGRATION-DOMAINS-V1-V3
            migration: domains-v1-to-v3
            entries:
              - source_path: legacy.yaml
                disposition: merged_into
                target_id: V3-AGG-GAME-GAME
                legacy_ids:
                  - old-behavior
                  - OLD-RULE-001
              - source_paths:
                  - legacy.yaml
                  - historical.yaml
                disposition: split_into
                target_ids:
                  - V3-AGG-GAME-GAME
                  - V3-AGG-GAME-OTHER
                legacy_ids:
                  - HISTORICAL-RULE-001
            """, true);

        try
        {
            MigrationMapValidator validator = new MigrationMapValidator();

            MigrationMapValidationResult result = validator.Validate(
                Path.Combine(projectPath, "Domains"),
                Path.Combine(projectPath, "Pattern Network"));

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, issue => issue.RuleId == "V3-MIGRATION-SOURCE-DUPLICATE");
        }
        finally
        {
            Directory.Delete(projectPath, true);
        }
    }

    private string CreateValidDomainMap()
    {
        return """
            schema_version: 3
            spec_kind: migration_map
            id: V3-MIGRATION-DOMAINS-V1-V3
            migration: domains-v1-to-v3
            entries:
              - source_path: legacy.yaml
                disposition: merged_into
                target_id: V3-AGG-GAME-GAME
                legacy_ids:
                  - old-behavior
                  - OLD-RULE-001
            """;
    }

    private string CreateTemporaryMigrationProject(string domainMap, bool includeSecondTarget = false)
    {
        string projectPath = Path.Combine(Path.GetTempPath(), $"migration-validator-{Guid.NewGuid():N}");
        string domainsPath = Path.Combine(projectPath, "Domains");
        string networkPath = Path.Combine(projectPath, "Pattern Network");
        Directory.CreateDirectory(domainsPath);
        Directory.CreateDirectory(networkPath);
        File.WriteAllText(Path.Combine(domainsPath, "migration-map.yaml"), domainMap);
        File.WriteAllText(Path.Combine(domainsPath, "legacy.yaml"), """
            use_case: old-behavior
            contract:
              rules:
                - rule_id: OLD-RULE-001
            """);
        File.WriteAllText(Path.Combine(domainsPath, "target.yaml"), """
            schema_version: 3
            spec_kind: aggregate_contract
            id: V3-AGG-GAME-GAME
            aggregate: Game
            """);
        if (includeSecondTarget)
        {
            File.WriteAllText(Path.Combine(domainsPath, "other-target.yaml"), """
                schema_version: 3
                spec_kind: aggregate_contract
                id: V3-AGG-GAME-OTHER
                aggregate: Other
                """);
        }

        File.WriteAllText(Path.Combine(networkPath, "doctrine.yaml"), """
            schema_version: 3
            doctrine:
              id: V3-PN-DOCTRINE-ROOT
            """);
        File.WriteAllText(Path.Combine(networkPath, "migration-map.yaml"), """
            schema_version: 3
            spec_kind: migration_map
            id: V3-MIGRATION-PATTERN-NETWORK-V1-V3
            migration: pattern-network-v1-to-v3
            entries:
              - source_path: doctrine.yaml
                disposition: replaced_by
                target_id: V3-PN-DOCTRINE-ROOT
                legacy_ids:
                  - PN-DOCTRINE-ROOT
            """);
        return projectPath;
    }
}
