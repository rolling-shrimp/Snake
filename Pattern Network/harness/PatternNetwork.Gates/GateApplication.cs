namespace PatternNetwork.Gates;

public class GateApplication
{
    public int Run(string[] arguments, TextWriter output, TextWriter error)
    {
        try
        {
            if (arguments.Length == 0)
            {
                WriteUsage(error);
                return 2;
            }

            string command = arguments[0];
            Dictionary<string, string> options = ParseOptions(arguments.Skip(1).ToArray());
            GateRunResult result;
            if (command == "validate-network")
            {
                string networkPath = GetRequiredOption(options, "--network");
                PatternNetworkValidator validator = new PatternNetworkValidator();
                PatternNetworkValidationResult validation = validator.Validate(networkPath);
                result = new GateRunResult(validation.Issues, !validation.IsValid);
            }
            else if (command == "validate-presentations")
            {
                string domainsPath = GetRequiredOption(options, "--domains");
                string projectRoot = GetRequiredOption(options, "--project-root");
                PresentationContractValidator validator = new PresentationContractValidator();
                PresentationContractValidationResult validation = validator.Validate(domainsPath, projectRoot);
                result = new GateRunResult(validation.Issues, validation.HasConfigurationError);
            }
            else if (command == "check-code")
            {
                string networkPath = GetRequiredOption(options, "--network");
                string solutionPath = GetRequiredOption(options, "--solution");
                options.TryGetValue("--node", out string? nodeFilter);
                CodeGateRunner runner = new CodeGateRunner();
                result = runner.CheckCode(networkPath, solutionPath, nodeFilter);
            }
            else
            {
                WriteUsage(error);
                return 2;
            }

            GateReportWriter reportWriter = new GateReportWriter();
            reportWriter.WriteText(result, output);
            if (options.TryGetValue("--json-output", out string? jsonOutputPath))
            {
                WriteJsonReport(jsonOutputPath, reportWriter.CreateJson(result));
            }

            if (result.HasConfigurationError)
            {
                return 2;
            }

            return result.IsSuccessful ? 0 : 1;
        }
        catch (ArgumentException exception)
        {
            error.WriteLine(exception.Message);
            WriteUsage(error);
            return 2;
        }
        catch (Exception exception)
        {
            error.WriteLine($"Pattern Network Gate 執行失敗：{exception.Message}");
            return 2;
        }
    }

    private Dictionary<string, string> ParseOptions(string[] arguments)
    {
        Dictionary<string, string> options = new Dictionary<string, string>(StringComparer.Ordinal);
        HashSet<string> supportedOptions = new HashSet<string>(StringComparer.Ordinal)
        {
            "--network",
            "--solution",
            "--node",
            "--domains",
            "--project-root",
            "--json-output"
        };
        int index = 0;
        while (index < arguments.Length)
        {
            string option = arguments[index];
            if (!supportedOptions.Contains(option))
            {
                throw new ArgumentException($"不支援的選項：{option}");
            }

            if (index + 1 >= arguments.Length || arguments[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"選項 {option} 缺少值。");
            }

            if (!options.TryAdd(option, arguments[index + 1]))
            {
                throw new ArgumentException($"選項 {option} 不得重複。");
            }

            index += 2;
        }

        return options;
    }

    private string GetRequiredOption(Dictionary<string, string> options, string option)
    {
        if (!options.TryGetValue(option, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"缺少必要選項：{option}");
        }

        return value;
    }

    private void WriteJsonReport(string jsonOutputPath, string json)
    {
        try
        {
            string fullPath = Path.GetFullPath(jsonOutputPath);
            string? directoryPath = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(fullPath, json);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"寫入 JSON 報告失敗：{jsonOutputPath}；{exception.Message}", exception);
        }
    }

    private void WriteUsage(TextWriter error)
    {
        error.WriteLine("Usage:");
        error.WriteLine("  validate-network --network <path> [--json-output <file>]");
        error.WriteLine("  validate-presentations --domains <path> --project-root <path> [--json-output <file>]");
        error.WriteLine("  check-code --network <path> --solution <solution> [--node <id>] [--json-output <file>]");
    }
}
