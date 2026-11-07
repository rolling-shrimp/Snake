using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PatternNetwork.Gates;

public class SemanticGateInspector
{
    public List<GateIssue> InspectApplicationContracts(
        PatternNodeDefinition applicationNode,
        string solutionPath,
        string domainProjectDirectory,
        string applicationProjectDirectory)
    {
        List<GateIssue> issues = new List<GateIssue>();
        if (!applicationNode.Checks.Any(check => string.Equals(check.Checker, "use_case_entity_exposure", StringComparison.Ordinal)))
        {
            return issues;
        }

        try
        {
            List<MetadataReference> platformReferences = LoadPlatformReferences();
            List<SyntaxTree> domainSyntaxTrees = LoadSyntaxTrees(domainProjectDirectory);
            CSharpCompilation domainCompilation = CSharpCompilation.Create(
                "PatternNetwork.Domain.SemanticInput",
                domainSyntaxTrees,
                platformReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            List<MetadataReference> applicationReferences = new List<MetadataReference>(platformReferences)
            {
                domainCompilation.ToMetadataReference()
            };
            List<SyntaxTree> applicationSyntaxTrees = LoadSyntaxTrees(applicationProjectDirectory);
            CSharpCompilation applicationCompilation = CSharpCompilation.Create(
                "PatternNetwork.Application.SemanticInput",
                applicationSyntaxTrees,
                applicationReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            ProjectNamePolicy projectNamePolicy = new ProjectNamePolicy();
            string domainNamespace = projectNamePolicy.GetProjectName(solutionPath, "Domain");
            foreach (SyntaxTree syntaxTree in applicationSyntaxTrees)
            {
                SemanticModel semanticModel = applicationCompilation.GetSemanticModel(syntaxTree, true);
                CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
                foreach (ClassDeclarationSyntax contractClass in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(IsPublicContractClass))
                {
                    ValidatePublicContractMembers(applicationNode, semanticModel, contractClass, domainNamespace, issues);
                }
            }
        }
        catch (Exception exception)
        {
            issues.Add(new GateIssue("V3-PN-SEMANTIC-ANALYSIS", applicationNode.Id, applicationProjectDirectory, null, $"Application 語意分析失敗：{exception.Message}", "error"));
        }

        return issues;
    }

    private void ValidatePublicContractMembers(
        PatternNodeDefinition applicationNode,
        SemanticModel semanticModel,
        ClassDeclarationSyntax contractClass,
        string domainNamespace,
        List<GateIssue> issues)
    {
        foreach (PropertyDeclarationSyntax property in contractClass.Members.OfType<PropertyDeclarationSyntax>().Where(member => member.Modifiers.Any(SyntaxKind.PublicKeyword)))
        {
            IPropertySymbol? propertySymbol = semanticModel.GetDeclaredSymbol(property);
            if (propertySymbol != null && ContainsDomainType(propertySymbol.Type, domainNamespace))
            {
                AddIssue(issues, applicationNode.Id, property, $"公開契約 {contractClass.Identifier.ValueText}.{property.Identifier.ValueText} 不得暴露 Domain 型別 {propertySymbol.Type.ToDisplayString()}。 ");
            }
        }

        foreach (FieldDeclarationSyntax field in contractClass.Members.OfType<FieldDeclarationSyntax>().Where(member => member.Modifiers.Any(SyntaxKind.PublicKeyword)))
        {
            foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
            {
                IFieldSymbol? fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                if (fieldSymbol != null && ContainsDomainType(fieldSymbol.Type, domainNamespace))
                {
                    AddIssue(issues, applicationNode.Id, variable, $"公開契約 {contractClass.Identifier.ValueText}.{variable.Identifier.ValueText} 不得暴露 Domain 型別 {fieldSymbol.Type.ToDisplayString()}。 ");
                }
            }
        }

        foreach (ConstructorDeclarationSyntax constructor in contractClass.Members.OfType<ConstructorDeclarationSyntax>().Where(member => member.Modifiers.Any(SyntaxKind.PublicKeyword)))
        {
            IMethodSymbol? constructorSymbol = semanticModel.GetDeclaredSymbol(constructor);
            if (constructorSymbol == null)
            {
                continue;
            }

            foreach (IParameterSymbol parameter in constructorSymbol.Parameters)
            {
                if (ContainsDomainType(parameter.Type, domainNamespace))
                {
                    AddIssue(issues, applicationNode.Id, constructor, $"公開契約建構式參數 {parameter.Name} 不得暴露 Domain 型別 {parameter.Type.ToDisplayString()}。 ");
                }
            }
        }
    }

    private bool IsPublicContractClass(ClassDeclarationSyntax classDeclaration)
    {
        string className = classDeclaration.Identifier.ValueText;
        return classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword)
            && (className.EndsWith("Command", StringComparison.Ordinal)
                || className.EndsWith("Query", StringComparison.Ordinal)
                || className.EndsWith("Result", StringComparison.Ordinal));
    }

    private bool ContainsDomainType(ITypeSymbol typeSymbol, string domainNamespace)
    {
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return ContainsDomainType(arrayType.ElementType, domainNamespace);
        }

        if (typeSymbol is INamedTypeSymbol namedType)
        {
            string containingNamespace = namedType.ContainingNamespace.ToDisplayString();
            if (string.Equals(containingNamespace, domainNamespace, StringComparison.Ordinal)
                || containingNamespace.StartsWith($"{domainNamespace}.", StringComparison.Ordinal))
            {
                return true;
            }

            foreach (ITypeSymbol typeArgument in namedType.TypeArguments)
            {
                if (ContainsDomainType(typeArgument, domainNamespace))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private List<SyntaxTree> LoadSyntaxTrees(string projectDirectory)
    {
        List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
        string[] sourceFiles = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);
        Array.Sort(sourceFiles, StringComparer.Ordinal);
        foreach (string sourceFile in sourceFiles)
        {
            string relativePath = Path.GetRelativePath(projectDirectory, sourceFile).Replace('\\', '/');
            if (relativePath.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
                || relativePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
                || sourceFile.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
                || sourceFile.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
                || sourceFile.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            syntaxTrees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(sourceFile), path: sourceFile));
        }

        return syntaxTrees;
    }

    private List<MetadataReference> LoadPlatformReferences()
    {
        string? trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            throw new InvalidOperationException("執行環境未提供 TRUSTED_PLATFORM_ASSEMBLIES。");
        }

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(assemblyPath => (MetadataReference)MetadataReference.CreateFromFile(assemblyPath))
            .ToList();
    }

    private void AddIssue(List<GateIssue> issues, string node, SyntaxNode syntaxNode, string message)
    {
        FileLinePositionSpan lineSpan = syntaxNode.GetLocation().GetLineSpan();
        issues.Add(new GateIssue("V3-PN-UC-PRO-005", node, lineSpan.Path, lineSpan.StartLinePosition.Line + 1, message.TrimEnd(), "error"));
    }
}
