using System.Text.Json;

namespace PatternNetwork.Gates;

public class GateReportWriter
{
    public void WriteText(GateRunResult result, TextWriter output)
    {
        if (result.IsSuccessful)
        {
            output.WriteLine("PASS: Pattern Network gate completed without violations.");
            return;
        }

        foreach (GateIssue issue in result.Issues)
        {
            string location = issue.Line.HasValue ? $"{issue.File}:{issue.Line.Value}" : issue.File;
            output.WriteLine($"{issue.Severity.ToUpperInvariant()} {issue.RuleId} [{issue.Node}] {location} - {issue.Message}");
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
