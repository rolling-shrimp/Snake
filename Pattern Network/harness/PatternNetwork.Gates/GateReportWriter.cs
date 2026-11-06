using System.Text.Json;

namespace PatternNetwork.Gates;

public class GateReportWriter
{
    public void WriteText(GateRunResult result, TextWriter output)
    {
        foreach (GateCheckResult check in result.Checks.Where(check => check.Status != VerificationStatus.Pass))
        {
            output.WriteLine(
                $"{check.Mode.ToString().ToUpperInvariant()} {check.Status.ToString().ToUpperInvariant()} {check.CheckId}");
        }

        foreach (GateIssue issue in result.Issues)
        {
            string location = issue.Line.HasValue ? $"{issue.File}:{issue.Line.Value}" : issue.File;
            output.WriteLine($"{issue.Severity.ToUpperInvariant()} {issue.RuleId} [{issue.Node}] {location} - {issue.Message}");
        }

        if (result.IsSuccessful)
        {
            output.WriteLine("PASS: Pattern Network gate completed without blocking violations.");
        }
    }

    public string CreateJson(GateRunResult result)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        object report = new
        {
            result.IsSuccessful,
            result.HasConfigurationError,
            Checks = result.Checks.Select(check => new
            {
                check.CheckId,
                Mode = check.Mode.ToString().ToLowerInvariant(),
                Status = check.Status.ToString().ToLowerInvariant(),
                check.RuleRefs,
                check.ScenarioRefs,
                check.Evidence
            }).ToList(),
            Issues = result.Issues.Select(issue => new
            {
                issue.RuleId,
                issue.Node,
                issue.File,
                issue.Line,
                issue.Message,
                issue.Severity
            }).ToList()
        };
        return JsonSerializer.Serialize(report, options);
    }
}
