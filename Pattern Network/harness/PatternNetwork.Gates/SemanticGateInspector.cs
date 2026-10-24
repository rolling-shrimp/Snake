using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PatternNetwork.Gates;

public class SemanticGateInspector
{
    public List<GateIssue> InspectUseCaseContracts(
        PatternNodeDefinition useCaseNode,
        string solutionPath,
        string entitiesProjectDirectory,
        string useCasesProjectDirectory)
    {
        List<GateIssue> issues = new List<GateIssue>();
        if (!useCaseNode.Checks.Any(check => string.Equals(check.Checker, "use_case_entity_exposure", StringComparison.Ordinal)))
        {
            return issues;
        }

        try
        {
            List<MetadataReference> platformReferences = LoadPlatformReferences();
            List<SyntaxTree> entitySyntaxTrees = LoadSyntaxTrees(entitiesProjectDirectory);
            CSharpCompilation entityCompilation = CSharpCompilation.Create(
                "PatternNetwork.Entities.SemanticInput",
                entitySyntaxTrees,
                platformReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            List<MetadataReference> useCaseReferences = new List<MetadataReference>(platformReferences)
            {
                entityCompilation.ToMetadataReference()
            };
            List<SyntaxTree> useCaseSyntaxTrees = LoadSyntaxTrees(useCasesProjectDirectory);
            CSharpCompilation useCaseCompilation = CSharpCompilation.Create(
                "PatternNetwork.UseCases.SemanticInput",
                useCaseSyntaxTrees,
                useCaseReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            ProjectNamePolicy projectNamePolicy = new ProjectNamePolicy();
            string entitiesNamespace = projectNamePolicy.GetProjectName(solutionPath, "Entities");
            foreach (SyntaxTree syntaxTree in useCaseSyntaxTrees)
            {
                SemanticModel semanticModel = useCaseCompilation.GetSemanticModel(syntaxTree, true);
                CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
                foreach (ClassDeclarationSyntax contractClass in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(IsPublicContractClass))
                {
                    ValidatePublicContractMembers(useCaseNode, semanticModel, contractClass, entitiesNamespace, issues);
                }
            }
        }
        catch (Exception exception)
        {
            issues.Add(new GateIssue("PN-SEMANTIC-ANALYSIS", useCaseNode.Id, useCasesProjectDirectory, null, $"Use Case 語意分析失敗：{exception.Message}", "error"));
        }

        return issues;
    }

    private void ValidatePublicContractMembers(
        PatternNodeDefinition useCaseNode,
        SemanticModel semanticModel,
        ClassDeclarationSyntax contractClass,
        string entitiesNamespace,
        List<GateIssue> issues)
    {
        foreach (PropertyDeclarationSyntax property in contractClass.Members.OfType<PropertyDeclarationSyntax>().Where(member => member.Modifiers.Any(SyntaxKind.PublicKeyword)))
        {
            IPropertySymbol? propertySymbol = semanticModel.GetDeclaredSymbol(property);
            if (propertySymbol != null && ContainsEntitiesType(propertySymbol.Type, entitiesNamespace))
            {
                AddIssue(issues, useCaseNode.Id, property, $"公開契約 {contractClass.Identifier.ValueText}.{property.Identifier.ValueText} 不得暴露 Entities 型別 {propertySymbol.Type.ToDisplayString()}。 ");
            }
        }

        foreach (FieldDeclarationSyntax field in contractClass.Members.OfType<FieldDeclarationSyntax>().Where(member => member.Modifiers.Any(SyntaxKind.PublicKeyword)))
        {
            foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
            {
                IFieldSymbol? fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                if (fieldSymbol != null && ContainsEntitiesType(fieldSymbol.Type, entitiesNamespace))
                {
                    AddIssue(issues, useCaseNode.Id, variable, $"公開契約 {contractClass.Identifier.ValueText}.{variable.Identifier.ValueText} 不得暴露 Entities 型別 {fieldSymbol.Type.ToDisplayString()}。 ");
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
                if (ContainsEntitiesType(parameter.Type, entitiesNamespace))
                {
                    AddIssue(issues, useCaseNode.Id, constructor, $"公開契約建構式參數 {parameter.Name} 不得暴露 Entities 型別 {parameter.Type.ToDisplayString()}。 ");
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

    private bool ContainsEntitiesType(ITypeSymbol typeSymbol, string entitiesNamespace)
    {
        if (typeSymbol is IArrayTypeSymbol arrayType)
        {
            return ContainsEntitiesType(arrayType.ElementType, entitiesNamespace);
        }

        if (typeSymbol is INamedTypeSymbol namedType)
        {
            string containingNamespace = namedType.ContainingNamespace.ToDisplayString();
            if (string.Equals(containingNamespace, entitiesNamespace, StringComparison.Ordinal)
                || containingNamespace.StartsWith($"{entitiesNamespace}.", StringComparison.Ordinal))
            {
                return true;
            }

            foreach (ITypeSymbol typeArgument in namedType.TypeArguments)
            {
                if (ContainsEntitiesType(typeArgument, entitiesNamespace))
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
        issues.Add(new GateIssue("PN-UC-PRO-005", node, lineSpan.Path, lineSpan.StartLinePosition.Line + 1, message.TrimEnd(), "error"));
    }
}
