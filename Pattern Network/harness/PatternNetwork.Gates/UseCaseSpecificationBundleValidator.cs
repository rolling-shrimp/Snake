using System.Text.RegularExpressions;

namespace PatternNetwork.Gates;

public class UseCaseSpecificationBundleValidator
{
    private static readonly List<string> RequiredFileNames = new List<string>
    {
        "frame.yaml",
        "domain_spec.yaml",
        "technical_spec.yaml",
        "acceptance.yaml"
    };

    public List<GateIssue> Validate(string domainsPath)
    {
        List<GateIssue> issues = new List<GateIssue>();
        if (!Directory.Exists(domainsPath))
        {
            issues.Add(CreateIssue(domainsPath, "找不到 Domains 規格目錄。", "V3-SPEC-USE-CASE-BUNDLE-PATH"));
            return issues;
        }

        foreach (string domainSpecificationPath in Directory.GetFiles(
                     domainsPath,
                     "domain_spec.yaml",
                     SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(domainSpecificationPath);
            if (!Regex.IsMatch(
                    source,
                    @"(?m)^\s*spec_kind:\s*domain_contract\s*(?:#.*)?$",
                    RegexOptions.CultureInvariant))
            {
                continue;
            }

            string useCaseDirectory = Path.GetDirectoryName(domainSpecificationPath)!;
            ValidateRequiredFiles(useCaseDirectory, issues);
            ValidateAdditionalYamlFiles(useCaseDirectory, issues);
        }

        return issues;
    }

    private void ValidateRequiredFiles(string useCaseDirectory, List<GateIssue> issues)
    {
        foreach (string requiredFileName in RequiredFileNames)
        {
            string requiredPath = Path.Combine(useCaseDirectory, requiredFileName);
            if (!File.Exists(requiredPath))
            {
                issues.Add(CreateIssue(
                    requiredPath,
                    $"Use Case 規格資料夾缺少 {requiredFileName}。",
                    "V3-SPEC-USE-CASE-BUNDLE-MISSING"));
            }
        }
    }

    private void ValidateAdditionalYamlFiles(string useCaseDirectory, List<GateIssue> issues)
    {
        HashSet<string> requiredFileNames = new HashSet<string>(RequiredFileNames, StringComparer.Ordinal);
        foreach (string yamlPath in Directory.GetFiles(useCaseDirectory, "*.yaml", SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileName(yamlPath);
            if (!requiredFileNames.Contains(fileName))
            {
                issues.Add(CreateIssue(
                    yamlPath,
                    $"Use Case 規格資料夾不得包含額外 YAML 檔案 {fileName}。",
                    "V3-SPEC-USE-CASE-BUNDLE-EXTRA"));
            }
        }
    }

    private GateIssue CreateIssue(string file, string message, string ruleId)
    {
        return new GateIssue(
            ruleId,
            "use-case-specification-bundle",
            file,
            null,
            message,
            "error");
    }
}
