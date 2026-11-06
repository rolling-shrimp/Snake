using System.Text.Json;
using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class GateCheckResultTests
{
    [Fact]
    public void AuditFailureIsReportedWithoutFailingTheGateRun()
    {
        GateIssue issue = new GateIssue(
            "V3-CODE-AUDIT-001",
            "UseCases",
            "SampleUseCase.cs",
            10,
            "仍使用一個 Use Case class 對應一個 Execute。",
            "warning");
        GateCheckResult check = new GateCheckResult(
            "code-architecture-audit",
            VerificationMode.Audit,
            VerificationStatus.Fail,
            new List<string> { "V3-PN-USECASES-001" },
            new List<string>(),
            new List<string>(),
            new List<GateIssue> { issue });

        GateRunResult result = new GateRunResult(new List<GateCheckResult> { check }, false);

        Assert.True(result.IsSuccessful);
        Assert.Single(result.Issues);
    }

    [Theory]
    [InlineData(VerificationStatus.Fail)]
    [InlineData(VerificationStatus.Pending)]
    public void BlockingFailureOrPendingEvidenceFailsTheGateRun(VerificationStatus status)
    {
        GateCheckResult check = new GateCheckResult(
            "specification-contracts",
            VerificationMode.Blocking,
            status,
            new List<string> { "V3-SPEC-001" },
            new List<string>(),
            new List<string>(),
            new List<GateIssue>());

        GateRunResult result = new GateRunResult(new List<GateCheckResult> { check }, false);

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void JsonReportIncludesCheckModeStatusAndEvidence()
    {
        GateCheckResult check = new GateCheckResult(
            "specification-contracts",
            VerificationMode.Blocking,
            VerificationStatus.Pass,
            new List<string> { "V3-SPEC-001" },
            new List<string> { "V3-ACC-001" },
            new List<string> { "validator:specification-contracts" },
            new List<GateIssue>());
        GateRunResult result = new GateRunResult(new List<GateCheckResult> { check }, false);
        GateReportWriter writer = new GateReportWriter();

        using JsonDocument report = JsonDocument.Parse(writer.CreateJson(result));

        JsonElement checkJson = report.RootElement.GetProperty("checks")[0];
        Assert.Equal("blocking", checkJson.GetProperty("mode").GetString());
        Assert.Equal("pass", checkJson.GetProperty("status").GetString());
        Assert.Equal("validator:specification-contracts", checkJson.GetProperty("evidence")[0].GetString());
    }

    [Fact]
    public void CheckerRegistryRunsOnlyExplicitlyRegisteredCheckers()
    {
        GateCheckerRegistry registry = new GateCheckerRegistry();
        registry.Register(
            "specification-contracts",
            inventory => new GateCheckResult(
                "specification-contracts",
                VerificationMode.Blocking,
                VerificationStatus.Pass,
                new List<string>(),
                new List<string>(),
                new List<string> { $"documents:{inventory.Documents.Count}" },
                new List<GateIssue>()));
        SpecificationInventory inventory = new SpecificationInventory(new List<SpecificationDocument>());

        List<GateCheckResult> results = registry.RunAll(inventory);

        GateCheckResult result = Assert.Single(results);
        Assert.Equal("specification-contracts", result.CheckId);
        Assert.Equal("documents:0", Assert.Single(result.Evidence));
    }
}
