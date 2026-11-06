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
            Dictionary<string, List<string>> options = ParseOptions(arguments.Skip(1).ToArray());
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
                string? nodeFilter = GetOptionalOption(options, "--node");
                CodeGateRunner runner = new CodeGateRunner();
                result = runner.CheckCode(networkPath, solutionPath, nodeFilter);
            }
            else if (command == "validate-specs")
            {
                string domainsPath = GetRequiredOption(options, "--domains");
                List<string> entryPaths = options.TryGetValue("--entry", out List<string>? entries)
                    ? entries
                    : new List<string>();
                SpecificationValidator validator = new SpecificationValidator();
                SpecificationValidationResult validation = validator.Validate(domainsPath, entryPaths);
                result = new GateRunResult(validation.Issues, validation.HasConfigurationError);
            }
            else if (command == "validate-migration")
            {
                string domainsPath = GetRequiredOption(options, "--domains");
                string networkPath = GetRequiredOption(options, "--network");
                MigrationMapValidator validator = new MigrationMapValidator();
                MigrationMapValidationResult validation = validator.Validate(domainsPath, networkPath);
                result = new GateRunResult(validation.Issues, validation.HasConfigurationError);
            }
            else if (command == "verify-project")
            {
                string networkPath = GetRequiredOption(options, "--network");
                string domainsPath = GetRequiredOption(options, "--domains");
                string misfitPath = GetRequiredOption(options, "--misfit");
                string solutionPath = GetRequiredOption(options, "--solution");
                string projectRoot = GetRequiredOption(options, "--project-root");
                string outputDirectory = GetRequiredOption(options, "--output-dir");
                ProjectVerificationRunner runner = new ProjectVerificationRunner();
                result = runner.Verify(
                    networkPath,
                    domainsPath,
                    misfitPath,
                    solutionPath,
                    projectRoot,
                    outputDirectory);
            }
            else
            {
                WriteUsage(error);
                return 2;
            }

            GateReportWriter reportWriter = new GateReportWriter();
            reportWriter.WriteText(result, output);
            string? jsonOutputPath = GetOptionalOption(options, "--json-output");
            if (!string.IsNullOrWhiteSpace(jsonOutputPath))
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

    private Dictionary<string, List<string>> ParseOptions(string[] arguments)
    {
        Dictionary<string, List<string>> options = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        HashSet<string> supportedOptions = new HashSet<string>(StringComparer.Ordinal)
        {
            "--network",
            "--solution",
            "--node",
            "--domains",
            "--project-root",
            "--json-output",
            "--entry",
            "--misfit",
            "--output-dir"
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

            if (!options.TryGetValue(option, out List<string>? values))
            {
                values = new List<string>();
                options.Add(option, values);
            }
            else if (option != "--entry")
            {
                throw new ArgumentException($"選項 {option} 不得重複。");
            }

            values.Add(arguments[index + 1]);

            index += 2;
        }

        return options;
    }

    private string GetRequiredOption(Dictionary<string, List<string>> options, string option)
    {
        string? value = GetOptionalOption(options, option);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"缺少必要選項：{option}");
        }

        return value;
    }

    private string? GetOptionalOption(Dictionary<string, List<string>> options, string option)
    {
        if (options.TryGetValue(option, out List<string>? values) && values.Count == 1)
        {
            return values[0];
        }

        return null;
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
        error.WriteLine("  validate-specs --domains <path> [--entry <file> ...] [--json-output <file>]");
        error.WriteLine("  validate-migration --domains <path> --network <path> [--json-output <file>]");
        error.WriteLine("  verify-project --network <path> --domains <path> --misfit <file> --solution <solution> --project-root <path> --output-dir <path> [--json-output <file>]");
    }
}
