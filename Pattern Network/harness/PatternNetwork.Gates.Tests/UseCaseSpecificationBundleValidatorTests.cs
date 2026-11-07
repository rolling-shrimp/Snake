using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class UseCaseSpecificationBundleValidatorTests
{
    [Fact]
    public void ValidateAcceptsExactlyFourUseCaseSpecificationFiles()
    {
        string domainsPath = CreateUseCaseSpecificationBundle(new Dictionary<string, string>
        {
            ["frame.yaml"] = "spec_kind: problem_frame",
            ["domain_spec.yaml"] = "spec_kind: domain_contract",
            ["technical_spec.yaml"] = "spec_kind: technical_contract",
            ["acceptance.yaml"] = "spec_kind: acceptance_contract"
        });

        try
        {
            UseCaseSpecificationBundleValidator validator = new UseCaseSpecificationBundleValidator();

            List<GateIssue> issues = validator.Validate(domainsPath);

            Assert.Empty(issues);
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsMissingRequiredUseCaseSpecificationFile()
    {
        string domainsPath = CreateUseCaseSpecificationBundle(new Dictionary<string, string>
        {
            ["frame.yaml"] = "spec_kind: problem_frame",
            ["domain_spec.yaml"] = "spec_kind: domain_contract",
            ["technical_spec.yaml"] = "spec_kind: technical_contract"
        });

        try
        {
            UseCaseSpecificationBundleValidator validator = new UseCaseSpecificationBundleValidator();

            List<GateIssue> issues = validator.Validate(domainsPath);

            Assert.Contains(issues, issue =>
                issue.RuleId == "V3-SPEC-USE-CASE-BUNDLE-MISSING"
                && issue.Message.Contains("acceptance.yaml", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    [Fact]
    public void ValidateRejectsAdditionalYamlFileInUseCaseSpecificationFolder()
    {
        string domainsPath = CreateUseCaseSpecificationBundle(new Dictionary<string, string>
        {
            ["frame.yaml"] = "spec_kind: problem_frame",
            ["domain_spec.yaml"] = "spec_kind: domain_contract",
            ["technical_spec.yaml"] = "spec_kind: technical_contract",
            ["acceptance.yaml"] = "spec_kind: acceptance_contract",
            ["workflow.yaml"] = "spec_kind: workflow_contract"
        });

        try
        {
            UseCaseSpecificationBundleValidator validator = new UseCaseSpecificationBundleValidator();

            List<GateIssue> issues = validator.Validate(domainsPath);

            Assert.Contains(issues, issue =>
                issue.RuleId == "V3-SPEC-USE-CASE-BUNDLE-EXTRA"
                && issue.Message.Contains("workflow.yaml", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(domainsPath, true);
        }
    }

    private string CreateUseCaseSpecificationBundle(Dictionary<string, string> files)
    {
        string domainsPath = Path.Combine(Path.GetTempPath(), $"v3-use-case-bundle-{Guid.NewGuid():N}");
        string useCasePath = Path.Combine(domainsPath, "Game", "move snake");
        Directory.CreateDirectory(useCasePath);
        foreach (KeyValuePair<string, string> file in files)
        {
            File.WriteAllText(Path.Combine(useCasePath, file.Key), file.Value);
        }

        return domainsPath;
    }
}
