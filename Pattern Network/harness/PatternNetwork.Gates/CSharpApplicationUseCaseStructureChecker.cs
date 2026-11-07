using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PatternNetwork.Gates;

public class CSharpApplicationUseCaseStructureChecker : IGateChecker
{
    private string _projectRoot;

    public CSharpApplicationUseCaseStructureChecker(string projectRoot)
    {
        _projectRoot = Path.GetFullPath(projectRoot);
    }

    public string Id => "csharp_application_use_case_structure";

    public GateCheckResult Check(SpecificationInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        List<GateIssue> issues = new List<GateIssue>();
        string? applicationPath = FindApplicationPath();
        if (applicationPath is null)
        {
            string expectedPath = Path.Combine(_projectRoot, "Core", "{SolutionName}.Application");
            issues.Add(CreateIssue(expectedPath, null, "找不到 Core/{SolutionName}.Application 專案目錄。"));
            return CreateResult(issues, new List<string>());
        }

        Dictionary<string, string> useCaseFiles = ValidateApplicationSources(applicationPath, issues);
        foreach (string operation in ReadOperations(inventory))
        {
            if (!useCaseFiles.ContainsKey(operation))
            {
                string expectedPath = Path.Combine(
                    applicationPath,
                    $"_{operation}UseCase",
                    $"{operation}UseCase.cs");
                issues.Add(CreateIssue(expectedPath, null, $"缺少 {operation}UseCase 的獨立目錄與同名類別。"));
            }
        }

        return CreateResult(
            issues,
            new List<string>
            {
                $"application-project:{applicationPath}",
                $"use-cases:{string.Join(",", useCaseFiles.Keys.OrderBy(name => name, StringComparer.Ordinal))}"
            });
    }

    private string? FindApplicationPath()
    {
        string corePath = Path.Combine(_projectRoot, "Core");
        if (!Directory.Exists(corePath))
        {
            return null;
        }

        return Directory.GetDirectories(corePath, "*.Application", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private Dictionary<string, string> ValidateApplicationSources(
        string applicationPath,
        List<GateIssue> issues)
    {
        Dictionary<string, string> useCaseFiles = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string file in Directory.GetFiles(applicationPath, "*.cs", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(applicationPath, file).Replace('\\', '/');
            if (relativePath.StartsWith("obj/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string source = File.ReadAllText(file);
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(source, path: file).GetCompilationUnitRoot();
            if (relativePath.StartsWith("_GameState/", StringComparison.Ordinal)
                || root.DescendantNodes().OfType<ClassDeclarationSyntax>().Any(classDeclaration =>
                    string.Equals(classDeclaration.Identifier.ValueText, "GameState", StringComparison.Ordinal)))
            {
                issues.Add(CreateIssue(
                    file,
                    1,
                    "Application 不得建立 _GameState 或 GameState；UseCase 必須透過 IGameRepository 取得 Domain Game。"));
            }

            foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                string className = classDeclaration.Identifier.ValueText;
                if (className.EndsWith("ApplicationService", StringComparison.Ordinal)
                    || string.Equals(className, "ProcessGameTickUseCase", StringComparison.Ordinal))
                {
                    issues.Add(CreateIssue(
                        file,
                        GetLine(classDeclaration),
                        $"不得使用 {className} 集中承載多個 Use Case。"));
                }

                if (!className.EndsWith("UseCase", StringComparison.Ordinal))
                {
                    continue;
                }

                string operation = className[..^"UseCase".Length];
                ValidateUseCaseFile(applicationPath, file, classDeclaration, operation, issues);
                if (!useCaseFiles.TryAdd(operation, file))
                {
                    issues.Add(CreateIssue(file, GetLine(classDeclaration), $"{operation}UseCase 不得重複宣告。"));
                }
            }

            ValidateRepositoryContract(applicationPath, file, root, issues);
            ValidateFeatureTypeLocation(applicationPath, file, root, issues);
        }

        return useCaseFiles;
    }

    private void ValidateUseCaseFile(
        string applicationPath,
        string file,
        ClassDeclarationSyntax classDeclaration,
        string operation,
        List<GateIssue> issues)
    {
        string expectedPath = Path.Combine(
            applicationPath,
            $"_{operation}UseCase",
            $"{operation}UseCase.cs");
        if (!string.Equals(Path.GetFullPath(file), Path.GetFullPath(expectedPath), StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(CreateIssue(
                file,
                GetLine(classDeclaration),
                $"{operation}UseCase 必須位於 _{operation}UseCase/{operation}UseCase.cs。"));
        }

        List<MethodDeclarationSyntax> publicMethods = classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(method => method.Modifiers.Any(modifier => modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
            .ToList();
        if (publicMethods.Count != 1
            || !string.Equals(publicMethods[0].Identifier.ValueText, operation, StringComparison.Ordinal))
        {
            issues.Add(CreateIssue(
                file,
                GetLine(classDeclaration),
                $"{operation}UseCase 必須只公開一個名為 {operation} 的行為方法。"));
        }

        IEnumerable<TypeSyntax> dependencyTypes = classDeclaration.Members
            .OfType<FieldDeclarationSyntax>()
            .Select(field => field.Declaration.Type)
            .Concat(classDeclaration.Members
                .OfType<ConstructorDeclarationSyntax>()
                .SelectMany(constructor => constructor.ParameterList.Parameters)
                .Where(parameter => parameter.Type is not null)
                .Select(parameter => parameter.Type!));
        foreach (TypeSyntax dependencyType in dependencyTypes)
        {
            string dependencyName = dependencyType.ToString().Split('.').Last();
            if (dependencyName.EndsWith("UseCase", StringComparison.Ordinal))
            {
                issues.Add(CreateIssue(
                    file,
                    GetLine(dependencyType),
                    $"{operation}UseCase 不得注入另一個 UseCase：{dependencyName}。"));
            }
        }

        bool injectsGameRepository = classDeclaration.Members
            .OfType<ConstructorDeclarationSyntax>()
            .SelectMany(constructor => constructor.ParameterList.Parameters)
            .Any(parameter => parameter.Type?.ToString().Split('.').Last() == "IGameRepository");
        if (!injectsGameRepository)
        {
            issues.Add(CreateIssue(
                file,
                GetLine(classDeclaration),
                $"{operation}UseCase 必須透過 constructor injection 取得 IGameRepository。"));
        }
    }

    private void ValidateRepositoryContract(
        string applicationPath,
        string file,
        CompilationUnitSyntax root,
        List<GateIssue> issues)
    {
        string relativePath = Path.GetRelativePath(applicationPath, file).Replace('\\', '/');
        foreach (InterfaceDeclarationSyntax interfaceDeclaration in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            if (!string.Equals(interfaceDeclaration.Identifier.ValueText, "IGameRepository", StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(relativePath, "Repositories/IGameRepository.cs", StringComparison.Ordinal))
            {
                issues.Add(CreateIssue(
                    file,
                    GetLine(interfaceDeclaration),
                    "IGameRepository 必須位於 Repositories/IGameRepository.cs。"));
            }
        }

        foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            if (classDeclaration.Identifier.ValueText.EndsWith("Repository", StringComparison.Ordinal))
            {
                issues.Add(CreateIssue(
                    file,
                    GetLine(classDeclaration),
                    $"Application 不得實作 {classDeclaration.Identifier.ValueText}；Repository 實作屬於 Infrastructure。"));
            }
        }
    }

    private void ValidateFeatureTypeLocation(
        string applicationPath,
        string file,
        CompilationUnitSyntax root,
        List<GateIssue> issues)
    {
        string relativePath = Path.GetRelativePath(applicationPath, file).Replace('\\', '/');
        string[] segments = relativePath.Split('/');
        bool isDtoDirectory = segments.Length == 3
            && segments[0].EndsWith("UseCase", StringComparison.Ordinal)
            && string.Equals(segments[1], "Dto", StringComparison.Ordinal);
        bool isEnumDirectory = segments.Length == 3
            && segments[0].EndsWith("UseCase", StringComparison.Ordinal)
            && string.Equals(segments[1], "Enums", StringComparison.Ordinal);
        foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            bool isDto = classDeclaration.Identifier.ValueText.EndsWith("DTO", StringComparison.Ordinal);
            if (isDtoDirectory && !isDto)
            {
                issues.Add(CreateIssue(
                    file,
                    GetLine(classDeclaration),
                    $"Dto 資料夾內的 class {classDeclaration.Identifier.ValueText} 必須以 DTO 結尾。"));
            }
            else if (isDto && !isDtoDirectory)
            {
                issues.Add(CreateIssue(
                    file,
                    GetLine(classDeclaration),
                    $"DTO 型別 {classDeclaration.Identifier.ValueText} 必須位於所屬 UseCase 的 Dto 子資料夾。"));
            }

            if (isEnumDirectory)
            {
                issues.Add(CreateIssue(
                    file,
                    GetLine(classDeclaration),
                    $"Enums 資料夾只能放 enum，不得放 class {classDeclaration.Identifier.ValueText}。"));
            }
        }

        foreach (EnumDeclarationSyntax enumDeclaration in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            if (!isEnumDirectory)
            {
                issues.Add(CreateIssue(
                    file,
                    GetLine(enumDeclaration),
                    $"UseCase enum {enumDeclaration.Identifier.ValueText} 必須位於該 UseCase 的 Enums 子資料夾。"));
            }
        }
    }

    private List<string> ReadOperations(SpecificationInventory inventory)
    {
        return inventory.Documents
            .Where(document => document.Header.SpecificationKind == SpecificationKind.TechnicalContract)
            .Select(document => ReadOperation(document.FilePath))
            .Where(operation => !string.IsNullOrWhiteSpace(operation))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(operation => operation, StringComparer.Ordinal)
            .ToList();
    }

    private string? ReadOperation(string specificationPath)
    {
        string source = File.ReadAllText(specificationPath);
        Match operation = Regex.Match(source, @"(?m)^operation:\s*(?<name>[A-Za-z][A-Za-z0-9]*)\s*$");
        return operation.Success ? operation.Groups["name"].Value : null;
    }

    private GateCheckResult CreateResult(List<GateIssue> issues, List<string> evidence)
    {
        return new GateCheckResult(
            Id,
            VerificationMode.Blocking,
            issues.Count == 0 ? VerificationStatus.Pass : VerificationStatus.Fail,
            new List<string>(),
            new List<string>(),
            evidence,
            issues);
    }

    private GateIssue CreateIssue(string file, int? line, string message)
    {
        return new GateIssue(
            "V3-CODE-APPLICATION-USE-CASE-STRUCTURE",
            "Application",
            file,
            line,
            message,
            "error");
    }

    private int GetLine(Microsoft.CodeAnalysis.SyntaxNode node)
    {
        return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    }
}
