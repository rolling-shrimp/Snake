using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PatternNetwork.Gates;

public class CSharpGateInspector
{
    public List<GateIssue> Inspect(PatternNodeDefinition patternNode, string projectDirectory)
    {
        List<GateIssue> issues = new List<GateIssue>();
        string[] sourceFiles;
        try
        {
            sourceFiles = Directory.GetFiles(projectDirectory, "*.cs", SearchOption.AllDirectories);
        }
        catch (Exception exception)
        {
            issues.Add(new GateIssue("V3-PN-SOURCE-READ", patternNode.Id, projectDirectory, null, $"列舉 C# 原始碼失敗：{exception.Message}", "error"));
            return issues;
        }

        Array.Sort(sourceFiles, StringComparer.Ordinal);
        foreach (string sourceFile in sourceFiles)
        {
            if (IsGeneratedFile(projectDirectory, sourceFile))
            {
                continue;
            }

            InspectFile(patternNode, projectDirectory, sourceFile, issues);
        }

        if (HasChecker(patternNode, "winforms_program"))
        {
            ValidateWinFormsProgram(patternNode, projectDirectory, issues);
        }

        if (HasChecker(patternNode, "dependency_injection"))
        {
            ValidateDependencyInjection(patternNode, projectDirectory, sourceFiles, issues);
        }

        if (HasChecker(patternNode, "application_feature_layout"))
        {
            ValidateApplicationFeatureLayout(patternNode, projectDirectory, sourceFiles, issues);
        }

        if (HasChecker(patternNode, "winforms_roles"))
        {
            ValidateWinFormsRoles(patternNode, projectDirectory, sourceFiles, issues);
        }

        return issues;
    }

    private void InspectFile(
        PatternNodeDefinition patternNode,
        string projectDirectory,
        string sourceFile,
        List<GateIssue> issues)
    {
        try
        {
            string sourceText = File.ReadAllText(sourceFile);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: sourceFile);
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
            foreach (Diagnostic diagnostic in syntaxTree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();
                issues.Add(new GateIssue("V3-PN-CS-SYNTAX", patternNode.Id, sourceFile, lineSpan.StartLinePosition.Line + 1, diagnostic.GetMessage(), "error"));
            }

            if (HasChecker(patternNode, "csharp_global_rules"))
            {
                ValidateGlobalRules(patternNode, sourceFile, root, issues);
            }

            ValidateForbiddenNamespaces(patternNode, sourceFile, root, issues);
            if (HasChecker(patternNode, "domain_state_boundaries"))
            {
                ValidateEntityBoundaries(patternNode, projectDirectory, sourceFile, root, issues);
            }

            if (HasChecker(patternNode, "application_use_case_contracts"))
            {
                ValidateApplicationUseCaseContracts(patternNode, sourceFile, root, issues);
            }

            if (HasChecker(patternNode, "winforms_roles"))
            {
                ValidateWinFormsBoundaries(patternNode, sourceFile, root, issues);
            }

            if (HasChecker(patternNode, "game_form_boundaries"))
            {
                ValidateGameFormBoundaries(patternNode, sourceFile, root, issues);
            }
        }
        catch (Exception exception)
        {
            issues.Add(new GateIssue("V3-PN-SOURCE-READ", patternNode.Id, sourceFile, null, $"讀取或分析 C# 原始碼失敗：{exception.Message}", "error"));
        }
    }

    private void ValidateGlobalRules(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
        foreach (FileScopedNamespaceDeclarationSyntax fileScopedNamespace in root.Members
            .OfType<FileScopedNamespaceDeclarationSyntax>())
        {
            AddIssue(
                issues,
                "V3-PN-CS-NAMESPACE-001",
                patternNode.Id,
                sourceFile,
                fileScopedNamespace,
                "namespace 必須使用區塊式大括號，不得使用 file-scoped namespace。");
        }

        foreach (RecordDeclarationSyntax record in root.DescendantNodes().OfType<RecordDeclarationSyntax>())
        {
            AddIssue(issues, "V3-PN-CS-CLASS-002", patternNode.Id, sourceFile, record, "手寫型別不得使用 record。 ");
        }

        List<ClassDeclarationSyntax> classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        if (classes.Count > 1)
        {
            AddIssue(issues, "V3-PN-CS-FILE-001", patternNode.Id, sourceFile, classes[1], "一個 .cs 檔案只能包含一個類別。 ");
        }

        foreach (ClassDeclarationSyntax classDeclaration in classes)
        {
            ValidateClassDeclaration(patternNode, sourceFile, classDeclaration, issues);
        }

        bool containsOnlyGameFormPartial = classes.Count == 1
            && string.Equals(classes[0].Identifier.ValueText, "GameForm", StringComparison.Ordinal)
            && Path.GetFileName(sourceFile).StartsWith("GameForm", StringComparison.Ordinal)
            && classes[0].Modifiers.Any(SyntaxKind.PartialKeyword);
        if (classes.Count == 1
            && !containsOnlyGameFormPartial
            && !string.Equals(Path.GetFileNameWithoutExtension(sourceFile), classes[0].Identifier.ValueText, StringComparison.Ordinal))
        {
            AddIssue(issues, "V3-PN-CS-FILE-001", patternNode.Id, sourceFile, classes[0], $"類別 {classes[0].Identifier.ValueText} 必須放在同名檔案。 ");
        }

        foreach (FieldDeclarationSyntax field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            ValidateField(patternNode, sourceFile, field, issues);
        }

        ValidateConstructorDependencyAssignments(patternNode, sourceFile, root, issues);

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            bool isProgramMain = string.Equals(Path.GetFileName(sourceFile), "Program.cs", StringComparison.Ordinal)
                && string.Equals(method.Identifier.ValueText, "Main", StringComparison.Ordinal);
            if (method.Modifiers.Any(SyntaxKind.StaticKeyword)
                && !IsDependencyInjectionRegistrationFile(sourceFile)
                && !isProgramMain)
            {
                AddIssue(issues, "V3-PN-CS-METHOD-001", patternNode.Id, sourceFile, method, $"方法 {method.Identifier.ValueText} 不得宣告 static。 ");
            }

            if (!IsPascalCase(method.Identifier.ValueText))
            {
                AddIssue(issues, "V3-PN-CS-NAME-001", patternNode.Id, sourceFile, method, $"方法 {method.Identifier.ValueText} 必須使用 PascalCase。 ");
            }
        }

        foreach (PropertyDeclarationSyntax property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (!IsPascalCase(property.Identifier.ValueText))
            {
                AddIssue(issues, "V3-PN-CS-NAME-001", patternNode.Id, sourceFile, property, $"屬性 {property.Identifier.ValueText} 必須使用 PascalCase。 ");
            }
        }

        foreach (EnumDeclarationSyntax enumDeclaration in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            if (!IsPascalCase(enumDeclaration.Identifier.ValueText))
            {
                AddIssue(issues, "V3-PN-CS-NAME-001", patternNode.Id, sourceFile, enumDeclaration, $"列舉 {enumDeclaration.Identifier.ValueText} 必須使用 PascalCase。 ");
            }
        }

        foreach (ParameterSyntax parameter in root.DescendantNodes().OfType<ParameterSyntax>())
        {
            if (!IsCamelCase(parameter.Identifier.ValueText))
            {
                AddIssue(issues, "V3-PN-CS-NAME-001", patternNode.Id, sourceFile, parameter, $"參數 {parameter.Identifier.ValueText} 必須使用 camelCase。 ");
            }
        }

        foreach (VariableDeclaratorSyntax variable in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (variable.Parent?.Parent is LocalDeclarationStatementSyntax && !IsCamelCase(variable.Identifier.ValueText))
            {
                AddIssue(issues, "V3-PN-CS-NAME-001", patternNode.Id, sourceFile, variable, $"區域變數 {variable.Identifier.ValueText} 必須使用 camelCase。 ");
            }
        }

        ValidateControlFlowBraces(patternNode, sourceFile, root, issues);
    }

    private void ValidateConstructorDependencyAssignments(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
        foreach (ConstructorDeclarationSyntax constructor in root.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
        {
            HashSet<string> parameterNames = constructor.ParameterList.Parameters
                .Select(parameter => parameter.Identifier.ValueText)
                .ToHashSet(StringComparer.Ordinal);
            foreach (AssignmentExpressionSyntax assignment in constructor.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (assignment.Right is not IdentifierNameSyntax parameterReference || !parameterNames.Contains(parameterReference.Identifier.ValueText))
                {
                    continue;
                }

                string fieldName = GetAssignedMemberName(assignment.Left);
                if (!fieldName.StartsWith("_", StringComparison.Ordinal))
                {
                    continue;
                }

                string expectedFieldName = $"_{parameterReference.Identifier.ValueText}";
                if (!string.Equals(fieldName, expectedFieldName, StringComparison.Ordinal))
                {
                    AddIssue(issues, "V3-PN-CS-DI-001", patternNode.Id, sourceFile, assignment, $"建構式參數 {parameterReference.Identifier.ValueText} 必須保存至 {expectedFieldName}，不得保存至 {fieldName}。 ");
                }
            }
        }
    }

    private string GetAssignedMemberName(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.ValueText;
        }

        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.ValueText;
        }

        return string.Empty;
    }

    private void ValidateClassDeclaration(PatternNodeDefinition patternNode, string sourceFile, ClassDeclarationSyntax classDeclaration, List<GateIssue> issues)
    {
        bool isProgramClass = string.Equals(Path.GetFileName(sourceFile), "Program.cs", StringComparison.Ordinal)
            && string.Equals(classDeclaration.Identifier.ValueText, "Program", StringComparison.Ordinal);
        bool isGameFormPartial = Path.GetFileName(sourceFile).StartsWith("GameForm", StringComparison.Ordinal)
            && string.Equals(Path.GetExtension(sourceFile), ".cs", StringComparison.Ordinal)
            && string.Equals(classDeclaration.Identifier.ValueText, "GameForm", StringComparison.Ordinal);
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword) && !isProgramClass)
        {
            AddIssue(issues, "V3-PN-CS-CLASS-001", patternNode.Id, sourceFile, classDeclaration, $"類別 {classDeclaration.Identifier.ValueText} 必須宣告 public。 ");
        }

        foreach (SyntaxKind forbiddenModifier in new[] { SyntaxKind.SealedKeyword, SyntaxKind.AbstractKeyword, SyntaxKind.PartialKeyword, SyntaxKind.StaticKeyword })
        {
            bool isAllowedRegistrationClass = forbiddenModifier == SyntaxKind.StaticKeyword
                && IsDependencyInjectionRegistrationFile(sourceFile)
                && string.Equals(classDeclaration.Identifier.ValueText, "DependencyInjection", StringComparison.Ordinal);
            bool isAllowedProgramClass = forbiddenModifier == SyntaxKind.StaticKeyword && isProgramClass;
            bool isAllowedGameFormPartial = forbiddenModifier == SyntaxKind.PartialKeyword && isGameFormPartial;
            if (classDeclaration.Modifiers.Any(forbiddenModifier)
                && !isAllowedRegistrationClass
                && !isAllowedProgramClass
                && !isAllowedGameFormPartial)
            {
                string modifierText = SyntaxFacts.GetText(forbiddenModifier);
                AddIssue(issues, "V3-PN-CS-CLASS-001", patternNode.Id, sourceFile, classDeclaration, $"手寫類別 {classDeclaration.Identifier.ValueText} 不得宣告 {modifierText}。 ");
            }
        }

        if (!IsPascalCase(classDeclaration.Identifier.ValueText))
        {
            AddIssue(issues, "V3-PN-CS-NAME-001", patternNode.Id, sourceFile, classDeclaration, $"類別 {classDeclaration.Identifier.ValueText} 必須使用 PascalCase。 ");
        }
    }

    private void ValidateField(PatternNodeDefinition patternNode, string sourceFile, FieldDeclarationSyntax field, List<GateIssue> issues)
    {
        bool isConst = field.Modifiers.Any(SyntaxKind.ConstKeyword);
        if (!field.Modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            AddIssue(issues, "V3-PN-CS-FIELD-001", patternNode.Id, sourceFile, field, "欄位必須宣告 private。 ");
        }

        if (field.Modifiers.Any(SyntaxKind.StaticKeyword) || field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            AddIssue(issues, "V3-PN-CS-FIELD-002", patternNode.Id, sourceFile, field, "欄位不得宣告 static 或 readonly。 ");
        }

        foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
        {
            if (isConst)
            {
                if (!Regex.IsMatch(variable.Identifier.ValueText, "^[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*$"))
                {
                    AddIssue(issues, "V3-PN-CS-CONST-001", patternNode.Id, sourceFile, variable, $"常數 {variable.Identifier.ValueText} 必須使用 UPPER_SNAKE_CASE。 ");
                }
            }
            else if (!Regex.IsMatch(variable.Identifier.ValueText, "^_[a-z][A-Za-z0-9]*$"))
            {
                AddIssue(issues, "V3-PN-CS-FIELD-001", patternNode.Id, sourceFile, variable, $"欄位 {variable.Identifier.ValueText} 必須使用 _camelCase。 ");
            }
        }
    }

    private void ValidateControlFlowBraces(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
        foreach (IfStatementSyntax statement in root.DescendantNodes().OfType<IfStatementSyntax>())
        {
            if (statement.Statement is not BlockSyntax)
            {
                AddIssue(issues, "V3-PN-CS-FLOW-001", patternNode.Id, sourceFile, statement.Statement, "if 或 else if 主體必須使用大括號。 ");
            }

            if (statement.Else != null && statement.Else.Statement is not BlockSyntax && statement.Else.Statement is not IfStatementSyntax)
            {
                AddIssue(issues, "V3-PN-CS-FLOW-001", patternNode.Id, sourceFile, statement.Else.Statement, "else 主體必須使用大括號。 ");
            }
        }

        foreach (ForStatementSyntax statement in root.DescendantNodes().OfType<ForStatementSyntax>())
        {
            ValidateLoopBody(patternNode, sourceFile, statement.Statement, issues);
        }

        foreach (ForEachStatementSyntax statement in root.DescendantNodes().OfType<ForEachStatementSyntax>())
        {
            ValidateLoopBody(patternNode, sourceFile, statement.Statement, issues);
        }

        foreach (WhileStatementSyntax statement in root.DescendantNodes().OfType<WhileStatementSyntax>())
        {
            ValidateLoopBody(patternNode, sourceFile, statement.Statement, issues);
        }

        foreach (DoStatementSyntax statement in root.DescendantNodes().OfType<DoStatementSyntax>())
        {
            ValidateLoopBody(patternNode, sourceFile, statement.Statement, issues);
        }
    }

    private void ValidateLoopBody(PatternNodeDefinition patternNode, string sourceFile, StatementSyntax body, List<GateIssue> issues)
    {
        if (body is not BlockSyntax)
        {
            AddIssue(issues, "V3-PN-CS-FLOW-001", patternNode.Id, sourceFile, body, "迴圈主體必須使用大括號。 ");
        }
    }

    private void ValidateForbiddenNamespaces(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
        GateCheckDefinition? check = patternNode.Checks.FirstOrDefault(candidate => string.Equals(candidate.Checker, "forbidden_namespaces", StringComparison.Ordinal));
        if (check == null || !check.ListParameters.TryGetValue("namespaces", out List<string>? forbiddenNamespaces))
        {
            return;
        }

        string ruleId = patternNode.Id switch
        {
            "domain" => "V3-PN-DOM-PRO-002",
            "infrastructure" => "V3-PN-INF-PRO-003",
            _ => "V3-PN-UC-PRO-002"
        };
        foreach (UsingDirectiveSyntax usingDirective in root.Usings)
        {
            string namespaceName = usingDirective.Name?.ToString() ?? string.Empty;
            foreach (string forbiddenNamespace in forbiddenNamespaces)
            {
                if (namespaceName.Equals(forbiddenNamespace, StringComparison.Ordinal) || namespaceName.StartsWith($"{forbiddenNamespace}.", StringComparison.Ordinal))
                {
                    AddIssue(issues, ruleId, patternNode.Id, sourceFile, usingDirective, $"禁止使用 namespace：{namespaceName}。 ");
                }
            }
        }
    }

    private void ValidateEntityBoundaries(
        PatternNodeDefinition patternNode,
        string projectDirectory,
        string sourceFile,
        CompilationUnitSyntax root,
        List<GateIssue> issues)
    {
        foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            if (classDeclaration.Identifier.ValueText.EndsWith("Id", StringComparison.Ordinal))
            {
                AddIssue(
                    issues,
                    "V3-PN-DOM-PRO-007",
                    patternNode.Id,
                    sourceFile,
                    classDeclaration,
                    $"規格未宣告 identity，不得建立獨立識別類別 {classDeclaration.Identifier.ValueText}。");
            }
        }

        string relativePath = Path.GetRelativePath(projectDirectory, sourceFile).Replace('\\', '/');
        foreach (EnumDeclarationSyntax enumDeclaration in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            string expectedPath = $"Enums/{enumDeclaration.Identifier.ValueText}.cs";
            if (!string.Equals(relativePath, expectedPath, StringComparison.Ordinal))
            {
                AddIssue(
                    issues,
                    "V3-PN-DOM-PRO-008",
                    patternNode.Id,
                    sourceFile,
                    enumDeclaration,
                    $"Domain enum {enumDeclaration.Identifier.ValueText} 必須位於 {expectedPath}。");
            }
        }

        foreach (PropertyDeclarationSyntax property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (!property.Modifiers.Any(SyntaxKind.PublicKeyword) || property.AccessorList == null)
            {
                continue;
            }

            foreach (AccessorDeclarationSyntax accessor in property.AccessorList.Accessors.Where(accessor => accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration)))
            {
                bool isRestricted = accessor.Modifiers.Any(SyntaxKind.PrivateKeyword) || accessor.Modifiers.Any(SyntaxKind.ProtectedKeyword) || accessor.Modifiers.Any(SyntaxKind.InternalKeyword);
                if (!isRestricted)
                {
                    AddIssue(issues, "V3-PN-DOM-PRO-004", patternNode.Id, sourceFile, accessor, $"領域屬性 {property.Identifier.ValueText} 不得提供 public {accessor.Keyword.ValueText}。 ");
                }
            }
        }

        foreach (MemberAccessExpressionSyntax memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            string expression = memberAccess.ToString();
            if (expression is "DateTime.Now" or "DateTime.UtcNow" or "Guid.NewGuid" || expression.StartsWith("Environment.", StringComparison.Ordinal))
            {
                AddIssue(issues, "V3-PN-DOM-PRO-003", patternNode.Id, sourceFile, memberAccess, $"Domain 不得直接取得非確定性來源：{expression}。 ");
            }
        }

        foreach (ObjectCreationExpressionSyntax objectCreation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (objectCreation.Type.ToString() is "Random" or "System.Random")
            {
                AddIssue(issues, "V3-PN-DOM-PRO-003", patternNode.Id, sourceFile, objectCreation, "Domain 不得自行建立 Random。 ");
            }
        }
    }

    private void ValidateApplicationUseCaseContracts(
        PatternNodeDefinition patternNode,
        string sourceFile,
        CompilationUnitSyntax root,
        List<GateIssue> issues)
    {
        foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            bool isUseCase = classDeclaration.Identifier.ValueText.EndsWith("UseCase", StringComparison.Ordinal);
            bool isApplicationService = classDeclaration.Identifier.ValueText.EndsWith("ApplicationService", StringComparison.Ordinal);
            bool isProcessGameTick = string.Equals(
                classDeclaration.Identifier.ValueText,
                "ProcessGameTickUseCase",
                StringComparison.Ordinal);
            if (isApplicationService || isProcessGameTick)
            {
                AddIssue(
                    issues,
                    "V3-PN-UC-PRO-007",
                    patternNode.Id,
                    sourceFile,
                    classDeclaration,
                    $"{classDeclaration.Identifier.ValueText} 不得集中承載多個領域敘事；每個敘事必須使用獨立 UseCase class。");
            }

            if (isUseCase)
            {
                string className = classDeclaration.Identifier.ValueText;
                string operationName = className[..^"UseCase".Length];
                List<MethodDeclarationSyntax> behaviorMethods = classDeclaration.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword))
                    .ToList();
                if (behaviorMethods.Count != 1)
                {
                    AddIssue(
                        issues,
                        "V3-PN-UC-007",
                        patternNode.Id,
                        sourceFile,
                        classDeclaration,
                        $"{className} 必須恰好公開一個領域行為方法，實際為 {behaviorMethods.Count} 個。");
                }

                foreach (MethodDeclarationSyntax behaviorMethod in behaviorMethods)
                {
                    if (!string.Equals(behaviorMethod.Identifier.ValueText, operationName, StringComparison.Ordinal))
                    {
                        AddIssue(
                            issues,
                            "V3-PN-UC-007",
                            patternNode.Id,
                            sourceFile,
                            behaviorMethod,
                            $"{className} 的公開行為方法必須命名為 {operationName}。");
                    }
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
                    if (dependencyType.ToString().Split('.').Last().EndsWith("UseCase", StringComparison.Ordinal))
                    {
                        AddIssue(
                            issues,
                            "V3-PN-UC-PRO-006",
                            patternNode.Id,
                            sourceFile,
                            dependencyType,
                            $"{className} 不得注入另一個 UseCase：{dependencyType}。");
                    }
                }
            }

            bool hasEmptyWrapperSuffix = new[]
            {
                "Command",
                "Query",
                "Request",
                "Response",
                "Result",
                "Outcome",
                "RspDTO"
            }.Any(suffix => classDeclaration.Identifier.ValueText.EndsWith(suffix, StringComparison.Ordinal));
            bool hasDataMember = classDeclaration.Members.Any(member =>
                member is PropertyDeclarationSyntax or FieldDeclarationSyntax);
            if (hasEmptyWrapperSuffix && !hasDataMember)
            {
                AddIssue(
                    issues,
                    "V3-PN-UC-PRO-003",
                    patternNode.Id,
                    sourceFile,
                    classDeclaration,
                    $"空的 {classDeclaration.Identifier.ValueText} 是不必要的契約包裝；請直接使用無參數、void 或自然型別。");
            }
        }
    }

    private void ValidateWinFormsBoundaries(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
        GateCheckDefinition? roleCheck = patternNode.Checks.FirstOrDefault(
            candidate => string.Equals(candidate.Checker, "winforms_roles", StringComparison.Ordinal));
        List<string> forbiddenRoleSuffixes = roleCheck?.ListParameters.GetValueOrDefault(
            "forbidden_role_suffixes",
            new List<string>()) ?? new List<string>();
        List<string> forbiddenRoleNames = roleCheck?.ListParameters.GetValueOrDefault(
            "forbidden_role_names",
            new List<string>()) ?? new List<string>();
        foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            string className = classDeclaration.Identifier.ValueText;
            bool isAllowedRole = string.Equals(className, "GameForm", StringComparison.Ordinal);
            if (forbiddenRoleNames.Contains(className, StringComparer.Ordinal))
            {
                AddIssue(
                    issues,
                    "V3-PN-UI-PRO-002",
                    patternNode.Id,
                    sourceFile,
                    classDeclaration,
                    $"UI 不得宣告中介角色 {className}；事件、UseCase 順序與畫面更新由 GameForm 擁有。");
            }

            string? forbiddenSuffix = forbiddenRoleSuffixes.FirstOrDefault(suffix =>
                className.EndsWith(suffix, StringComparison.Ordinal));
            if (!isAllowedRole && forbiddenSuffix is not null)
            {
                AddIssue(
                    issues,
                    "V3-PN-UI-PRO-007",
                    patternNode.Id,
                    sourceFile,
                    classDeclaration,
                    $"UI 類別 {className} 不得使用意義不明的 {forbiddenSuffix} 角色名稱。");
            }

            if (!string.Equals(className, "GameForm", StringComparison.Ordinal))
            {
                continue;
            }

            List<string> directionKeys = new List<string> { "Up", "Down", "Left", "Right" };
            bool referencesDirectionKey = classDeclaration.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Any(memberAccess => IsMemberAccess(memberAccess, "Keys", directionKeys));
            bool declaresDirectionEvent = classDeclaration.Members
                .OfType<EventFieldDeclarationSyntax>()
                .SelectMany(eventField => eventField.Declaration.Variables)
                .Any(variable => string.Equals(
                    variable.Identifier.ValueText,
                    "DirectionRequested",
                    StringComparison.Ordinal));
            if (referencesDirectionKey || declaresDirectionEvent)
            {
                ValidateDirectionInputForm(
                    patternNode,
                    sourceFile,
                    classDeclaration,
                    "DirectionRequested",
                    "Direction",
                    directionKeys,
                    issues);
            }
        }

        foreach (InterfaceDeclarationSyntax interfaceDeclaration in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            string interfaceName = interfaceDeclaration.Identifier.ValueText;
            if (forbiddenRoleNames.Contains(interfaceName, StringComparer.Ordinal))
            {
                AddIssue(
                    issues,
                    "V3-PN-UI-PRO-002",
                    patternNode.Id,
                    sourceFile,
                    interfaceDeclaration,
                    $"UI 不得宣告中介角色 {interfaceName}；GameForm 直接擁有 UI 契約。");
            }
        }
    }

    private void ValidateDirectionInputForm(
        PatternNodeDefinition patternNode,
        string sourceFile,
        ClassDeclarationSyntax form,
        string directionDispatchEvent,
        string directionValueType,
        List<string> declaredDirectionKeys,
        List<GateIssue> issues)
    {
        MethodDeclarationSyntax? processCmdKey = form.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(method => string.Equals(
                method.Identifier.ValueText,
                "ProcessCmdKey",
                StringComparison.Ordinal));
        if (processCmdKey is null)
        {
            AddIssue(
                issues,
                "V3-PN-UI-008",
                patternNode.Id,
                sourceFile,
                form,
                "宣告方向鍵輸入的 Form 必須覆寫 ProcessCmdKey，在控制項導覽前派送輸入。 ");
        }
        else
        {
            ValidateProcessCmdKey(
                patternNode,
                sourceFile,
                processCmdKey,
                directionDispatchEvent,
                directionValueType,
                declaredDirectionKeys,
                issues);
        }

        foreach (MethodDeclarationSyntax parallelMethod in form.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(method => !string.Equals(
                method.Identifier.ValueText,
                "ProcessCmdKey",
                StringComparison.Ordinal)))
        {
            if (CountEventDispatches(parallelMethod, directionDispatchEvent) == 0)
            {
                continue;
            }

            AddIssue(
                issues,
                "V3-PN-UI-008",
                patternNode.Id,
                sourceFile,
                parallelMethod,
                $"方向輸入事件 {directionDispatchEvent} 只能由 ProcessCmdKey 派送，不得保留平行 KeyDown 或其他鍵盤事件路徑。 ");
        }
    }

    private void ValidateProcessCmdKey(
        PatternNodeDefinition patternNode,
        string sourceFile,
        MethodDeclarationSyntax processCmdKey,
        string directionDispatchEvent,
        string directionValueType,
        List<string> declaredDirectionKeys,
        List<GateIssue> issues)
    {
        bool hasRequiredSignature = processCmdKey.Modifiers.Any(SyntaxKind.ProtectedKeyword)
            && processCmdKey.Modifiers.Any(SyntaxKind.OverrideKeyword)
            && string.Equals(processCmdKey.ReturnType.ToString(), "bool", StringComparison.Ordinal);
        if (!hasRequiredSignature)
        {
            AddIssue(
                issues,
                "V3-PN-UI-008",
                patternNode.Id,
                sourceFile,
                processCmdKey,
                "ProcessCmdKey 必須使用 protected override bool 簽章。 ");
        }

        SwitchExpressionSyntax? directionSwitch = processCmdKey.DescendantNodes()
            .OfType<SwitchExpressionSyntax>()
            .FirstOrDefault(candidate => candidate.Arms.Any(arm =>
                GetMemberName(arm.Pattern, "Keys") is not null));
        if (directionSwitch is null
            || !directionSwitch.GoverningExpression.ToString().Contains(
                "Keys.KeyCode",
                StringComparison.Ordinal)
            || !HasExactDirectionMappings(
                directionSwitch,
                directionValueType,
                declaredDirectionKeys))
        {
            AddIssue(
                issues,
                "V3-PN-UI-008",
                patternNode.Id,
                sourceFile,
                processCmdKey,
                $"ProcessCmdKey 必須以 Keys.KeyCode 恰好映射 {string.Join("、", declaredDirectionKeys)} 到對應方向值。 ");
        }

        int dispatchCount = CountEventDispatches(processCmdKey, directionDispatchEvent);
        if (dispatchCount != 1)
        {
            AddIssue(
                issues,
                "V3-PN-UI-008",
                patternNode.Id,
                sourceFile,
                processCmdKey,
                $"ProcessCmdKey 必須恰好包含一次 {directionDispatchEvent} 派送，實際為 {dispatchCount} 次。 ");
        }

        bool returnsHandled = processCmdKey.DescendantNodes()
            .OfType<ReturnStatementSyntax>()
            .Any(returnStatement => returnStatement.Expression is LiteralExpressionSyntax literal
                && literal.IsKind(SyntaxKind.TrueLiteralExpression));
        bool delegatesUndeclaredInput = processCmdKey.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Expression is BaseExpressionSyntax
                && string.Equals(
                    memberAccess.Name.Identifier.ValueText,
                    "ProcessCmdKey",
                    StringComparison.Ordinal));
        if (!returnsHandled || !delegatesUndeclaredInput)
        {
            AddIssue(
                issues,
                "V3-PN-UI-008",
                patternNode.Id,
                sourceFile,
                processCmdKey,
                "宣告方向鍵派送後必須回傳 true；未宣告輸入必須委派 base.ProcessCmdKey。 ");
        }
    }

    private bool HasExactDirectionMappings(
        SwitchExpressionSyntax directionSwitch,
        string directionValueType,
        List<string> declaredDirectionKeys)
    {
        List<KeyValuePair<string, string>> mappings = new List<KeyValuePair<string, string>>();
        foreach (SwitchExpressionArmSyntax arm in directionSwitch.Arms)
        {
            string? keyName = GetMemberName(arm.Pattern, "Keys");
            string? directionName = GetMemberName(arm.Expression, directionValueType);
            string? vectorDirectionName = GetVectorDirectionName(arm.Expression);
            string? mappedDirectionName = directionName ?? vectorDirectionName;
            if (keyName is null && mappedDirectionName is null)
            {
                continue;
            }

            if (keyName is null || mappedDirectionName is null)
            {
                return false;
            }

            mappings.Add(new KeyValuePair<string, string>(keyName, mappedDirectionName));
        }

        if (mappings.Count != declaredDirectionKeys.Count
            || mappings.Select(mapping => mapping.Key).Distinct(StringComparer.Ordinal).Count()
                != declaredDirectionKeys.Count)
        {
            return false;
        }

        return declaredDirectionKeys.All(key => mappings.Any(mapping =>
            string.Equals(mapping.Key, key, StringComparison.Ordinal)
            && string.Equals(mapping.Value, key, StringComparison.Ordinal)));
    }

    private string? GetVectorDirectionName(ExpressionSyntax expression)
    {
        ObjectCreationExpressionSyntax? objectCreation = expression.DescendantNodesAndSelf()
            .OfType<ObjectCreationExpressionSyntax>()
            .FirstOrDefault(creation => creation.Type.ToString().Split('.').Last() == "SnakeDirectionDTO");
        if (objectCreation?.ArgumentList?.Arguments.Count != 2)
        {
            return null;
        }

        string x = objectCreation.ArgumentList.Arguments[0].Expression.ToString();
        string y = objectCreation.ArgumentList.Arguments[1].Expression.ToString();
        return (x, y) switch
        {
            ("0", "-1") => "Up",
            ("0", "1") => "Down",
            ("-1", "0") => "Left",
            ("1", "0") => "Right",
            _ => null
        };
    }

    private string? GetMemberName(SyntaxNode node, string ownerName)
    {
        return node.DescendantNodesAndSelf()
            .OfType<MemberAccessExpressionSyntax>()
            .FirstOrDefault(memberAccess => string.Equals(
                memberAccess.Expression.ToString(),
                ownerName,
                StringComparison.Ordinal))
            ?.Name.Identifier.ValueText;
    }

    private int CountEventDispatches(SyntaxNode node, string eventName)
    {
        int conditionalDispatches = node.DescendantNodes()
            .OfType<ConditionalAccessExpressionSyntax>()
            .Count(conditionalAccess => conditionalAccess.Expression is IdentifierNameSyntax identifier
                && string.Equals(identifier.Identifier.ValueText, eventName, StringComparison.Ordinal)
                && conditionalAccess.WhenNotNull.DescendantNodesAndSelf()
                    .OfType<InvocationExpressionSyntax>()
                    .Any());
        int directDispatches = node.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Count(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Expression is IdentifierNameSyntax identifier
                && string.Equals(identifier.Identifier.ValueText, eventName, StringComparison.Ordinal));
        return conditionalDispatches + directDispatches;
    }

    private bool IsWinFormsForm(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.BaseList?.Types.Any(baseType =>
            string.Equals(
                baseType.Type.ToString().Split('.').Last(),
                "Form",
                StringComparison.Ordinal)) == true;
    }

    private bool IsMemberAccess(
        MemberAccessExpressionSyntax memberAccess,
        string ownerName,
        List<string> memberNames)
    {
        return string.Equals(memberAccess.Expression.ToString(), ownerName, StringComparison.Ordinal)
            && memberNames.Contains(memberAccess.Name.Identifier.ValueText, StringComparer.Ordinal);
    }

    private void ValidateGameFormBoundaries(
        PatternNodeDefinition patternNode,
        string sourceFile,
        CompilationUnitSyntax root,
        List<GateIssue> issues)
    {
        foreach (ClassDeclarationSyntax form in root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(type => string.Equals(type.Identifier.ValueText, "GameForm", StringComparison.Ordinal)))
        {
            MethodDeclarationSyntax? tick = form.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(method => string.Equals(method.Identifier.ValueText, "ExecuteGameTick", StringComparison.Ordinal));
            if (tick is not null)
            {
                int moveCalls = tick.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Count(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess
                        && string.Equals(memberAccess.Name.Identifier.ValueText, "MoveSnake", StringComparison.Ordinal));
                if (moveCalls != 1)
                {
                    AddIssue(
                        issues,
                        "V3-PN-UI-009",
                        patternNode.Id,
                        sourceFile,
                        tick,
                        $"GameForm.ExecuteGameTick 一次只能呼叫一次 MoveSnakeUseCase.MoveSnake，實際為 {moveCalls} 次。");
                }

                if (tick.DescendantNodes().Any(node =>
                        node is ForStatementSyntax
                            or ForEachStatementSyntax
                            or WhileStatementSyntax
                            or DoStatementSyntax))
                {
                    AddIssue(issues, "V3-PN-UI-PRO-009", patternNode.Id, sourceFile, tick, "GameForm.ExecuteGameTick 不得用迴圈補跑移動。");
                }

                string tickSource = tick.ToString();
                if (new[] { "DateTime", "Stopwatch", "Elapsed", "elapsed", "accumulated", "deltaTime" }
                    .Any(token => tickSource.Contains(token, StringComparison.Ordinal)))
                {
                    AddIssue(issues, "V3-PN-UI-PRO-009", patternNode.Id, sourceFile, tick, "GameForm.ExecuteGameTick 不得讀取或累積時間。");
                }
            }

            MethodDeclarationSyntax? applyScreen = form.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(method => string.Equals(method.Identifier.ValueText, "ApplyScreen", StringComparison.Ordinal));
            if (applyScreen is not null)
            {
                string applyScreenSource = applyScreen.ToString();
                string? forbiddenOperation = new[] { "Controls.Clear", "ResumeLayout(true)", "_boardSurface.Invalidate" }
                    .FirstOrDefault(token => applyScreenSource.Contains(token, StringComparison.Ordinal));
                if (forbiddenOperation is not null)
                {
                    AddIssue(
                        issues,
                        "V3-PN-UI-PRO-006",
                        patternNode.Id,
                        sourceFile,
                        applyScreen,
                        $"GameForm.ApplyScreen 不得執行整體畫面更新：{forbiddenOperation}。");
                }

                if (!applyScreenSource.Contains("HasCellChanged", StringComparison.Ordinal))
                {
                    AddIssue(
                        issues,
                        "V3-PN-UI-004",
                        patternNode.Id,
                        sourceFile,
                        applyScreen,
                        "GameForm.ApplyScreen 必須先比對棋盤格差異，再更新變動格位。");
                }
            }
        }
    }

    private void ValidateWinFormsRoles(
        PatternNodeDefinition patternNode,
        string projectDirectory,
        string[] sourceFiles,
        List<GateIssue> issues)
    {
        GateCheckDefinition roleCheck = patternNode.Checks.First(candidate =>
            string.Equals(candidate.Checker, "winforms_roles", StringComparison.Ordinal));
        string formClass = roleCheck.ScalarParameters.GetValueOrDefault("form_class", "GameForm");
        List<string> forbiddenRoleNames = roleCheck.ListParameters.GetValueOrDefault(
            "forbidden_role_names",
            new List<string>());
        List<ClassDeclarationSyntax> formDeclarations = new List<ClassDeclarationSyntax>();
        List<(string Name, string File)> forbiddenDeclarations = new List<(string Name, string File)>();
        foreach (string sourceFile in sourceFiles.Where(sourceFile => !IsGeneratedFile(projectDirectory, sourceFile)))
        {
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(
                File.ReadAllText(sourceFile),
                path: sourceFile).GetCompilationUnitRoot();
            foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                string className = classDeclaration.Identifier.ValueText;
                if (string.Equals(className, formClass, StringComparison.Ordinal))
                {
                    formDeclarations.Add(classDeclaration);
                }

                if (forbiddenRoleNames.Contains(className, StringComparer.Ordinal))
                {
                    forbiddenDeclarations.Add((className, sourceFile));
                }
            }

            foreach (InterfaceDeclarationSyntax interfaceDeclaration in root.DescendantNodes().OfType<InterfaceDeclarationSyntax>())
            {
                string interfaceName = interfaceDeclaration.Identifier.ValueText;
                if (forbiddenRoleNames.Contains(interfaceName, StringComparer.Ordinal))
                {
                    forbiddenDeclarations.Add((interfaceName, sourceFile));
                }
            }
        }

        if (formDeclarations.Count == 0)
        {
            issues.Add(new GateIssue(
                "V3-PN-UI-001",
                patternNode.Id,
                projectDirectory,
                null,
                $"WinForms 必須宣告 {formClass}。",
                "error"));
        }

        foreach ((string forbiddenRoleName, string sourceFile) in forbiddenDeclarations)
        {
            issues.Add(new GateIssue(
                "V3-PN-UI-PRO-002",
                patternNode.Id,
                sourceFile,
                null,
                $"WinForms 不得宣告 {forbiddenRoleName}；事件、UseCase 順序與畫面更新由 {formClass} 擁有。",
                "error"));
        }

        bool hasTickWorkflow = formDeclarations
            .SelectMany(declaration => declaration.Members.OfType<MethodDeclarationSyntax>())
            .Any(method => string.Equals(method.Identifier.ValueText, "ExecuteGameTick", StringComparison.Ordinal));
        bool hasChangedScreenApplication = formDeclarations
            .SelectMany(declaration => declaration.Members.OfType<MethodDeclarationSyntax>())
            .Any(method => string.Equals(method.Identifier.ValueText, "ApplyScreen", StringComparison.Ordinal));
        bool injectsMoveSnakeUseCase = formDeclarations
            .SelectMany(declaration => declaration.Members.OfType<ConstructorDeclarationSyntax>())
            .SelectMany(constructor => constructor.ParameterList.Parameters)
            .Any(parameter => string.Equals(parameter.Type?.ToString().Split('.').Last(), "MoveSnakeUseCase", StringComparison.Ordinal));
        if (!hasTickWorkflow || !hasChangedScreenApplication || !injectsMoveSnakeUseCase)
        {
            issues.Add(new GateIssue(
                "V3-PN-UI-002",
                patternNode.Id,
                projectDirectory,
                null,
                $"{formClass} 必須透過 DI 取得 MoveSnakeUseCase，並擁有 ExecuteGameTick 與 ApplyScreen。",
                "error"));
        }
    }

    private void ValidateDependencyInjection(
        PatternNodeDefinition patternNode,
        string projectDirectory,
        string[] sourceFiles,
        List<GateIssue> issues)
    {
        List<string> inspectedSourceFiles = sourceFiles
            .Where(sourceFile => !IsGeneratedFile(projectDirectory, sourceFile))
            .ToList();
        foreach (string sourceFile in inspectedSourceFiles)
        {
            string sourceText = File.ReadAllText(sourceFile);
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(sourceText, path: sourceFile).GetCompilationUnitRoot();
            foreach (IdentifierNameSyntax serviceProviderType in root.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Where(identifier => string.Equals(identifier.Identifier.ValueText, "IServiceProvider", StringComparison.Ordinal)))
            {
                AddIssue(
                    issues,
                    "V3-PN-CS-DI-003",
                    patternNode.Id,
                    sourceFile,
                    serviceProviderType,
                    "不得注入或保存 IServiceProvider；依賴必須透過 constructor injection 明確宣告。");
            }

            bool isCompositionRoot = string.Equals(Path.GetFileName(sourceFile), "Program.cs", StringComparison.Ordinal);
            if (!isCompositionRoot)
            {
                foreach (InvocationExpressionSyntax serviceLocation in root.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax memberAccess
                        && memberAccess.Name.Identifier.ValueText is "GetService" or "GetRequiredService"))
                {
                    AddIssue(
                        issues,
                        "V3-PN-CS-DI-003",
                        patternNode.Id,
                        sourceFile,
                        serviceLocation,
                        "GetService/GetRequiredService 只能出現在 Program.cs composition root，不得作為 Service Locator。");
                }
            }

            foreach (ObjectCreationExpressionSyntax objectCreation in root.DescendantNodes()
                .OfType<ObjectCreationExpressionSyntax>()
                .Where(objectCreation => objectCreation.Type.ToString().Split('.').Last().EndsWith("UseCase", StringComparison.Ordinal)))
            {
                AddIssue(
                    issues,
                    "V3-PN-CS-DI-002",
                    patternNode.Id,
                    sourceFile,
                    objectCreation,
                    $"不得直接 new {objectCreation.Type}；UseCase 必須由 DI container 建立。");
            }
        }

        if (string.Equals(patternNode.Id, "application", StringComparison.Ordinal))
        {
            ValidateApplicationRegistrations(patternNode, projectDirectory, inspectedSourceFiles, issues);
        }
        else if (string.Equals(patternNode.Id, "infrastructure", StringComparison.Ordinal))
        {
            ValidateInfrastructureRegistrations(patternNode, projectDirectory, issues);
        }
        else if (string.Equals(patternNode.Id, "winforms", StringComparison.Ordinal))
        {
            ValidateWinFormsCompositionRoot(patternNode, projectDirectory, inspectedSourceFiles, issues);
        }
    }

    private void ValidateInfrastructureRegistrations(
        PatternNodeDefinition patternNode,
        string projectDirectory,
        List<GateIssue> issues)
    {
        GateCheckDefinition check = patternNode.Checks.First(candidate =>
            string.Equals(candidate.Checker, "dependency_injection", StringComparison.Ordinal));
        string registrationFile = check.ScalarParameters.GetValueOrDefault("registration_file", "DependencyInjection.cs");
        string registrationMethod = check.ScalarParameters.GetValueOrDefault("registration_method", "AddInfrastructure");
        string repositoryInterface = check.ScalarParameters.GetValueOrDefault("repository_interface", "IGameRepository");
        string repositoryImplementation = check.ScalarParameters.GetValueOrDefault("repository_implementation", "GameRepository");
        string registrationPath = Path.Combine(projectDirectory, registrationFile);
        if (!File.Exists(registrationPath))
        {
            issues.Add(new GateIssue(
                "V3-PN-INF-002",
                patternNode.Id,
                registrationPath,
                null,
                $"Infrastructure 必須提供 {registrationFile} 與 {registrationMethod}。",
                "error"));
            return;
        }

        string registrationText = File.ReadAllText(registrationPath);
        if (!Regex.IsMatch(registrationText, $@"\b{Regex.Escape(registrationMethod)}\s*\("))
        {
            issues.Add(new GateIssue(
                "V3-PN-INF-002",
                patternNode.Id,
                registrationPath,
                1,
                $"{registrationFile} 必須提供 {registrationMethod} registration method。",
                "error"));
        }

        string singletonPattern = $@"AddSingleton\s*<\s*{Regex.Escape(repositoryInterface)}\s*,\s*{Regex.Escape(repositoryImplementation)}\s*>";
        if (!Regex.IsMatch(registrationText, singletonPattern))
        {
            issues.Add(new GateIssue(
                "V3-PN-INF-PRO-002",
                patternNode.Id,
                registrationPath,
                1,
                $"{repositoryInterface}/{repositoryImplementation} 必須以 AddSingleton 註冊，確保所有 UseCase 共用同一個 Game。",
                "error"));
        }

        string repositoryPath = Path.Combine(
            projectDirectory,
            "Repositories",
            "_Game",
            $"{repositoryImplementation}.cs");
        if (!File.Exists(repositoryPath))
        {
            issues.Add(new GateIssue(
                "V3-PN-INF-001",
                patternNode.Id,
                repositoryPath,
                null,
                $"{repositoryImplementation} 必須位於 Repositories/_Game/{repositoryImplementation}.cs。",
                "error"));
        }
    }

    private void ValidateApplicationRegistrations(
        PatternNodeDefinition patternNode,
        string projectDirectory,
        List<string> sourceFiles,
        List<GateIssue> issues)
    {
        GateCheckDefinition check = patternNode.Checks.First(candidate =>
            string.Equals(candidate.Checker, "dependency_injection", StringComparison.Ordinal));
        string registrationFile = check.ScalarParameters.GetValueOrDefault("registration_file", "DependencyInjection.cs");
        string registrationMethod = check.ScalarParameters.GetValueOrDefault("registration_method", "AddApplication");
        string registrationPath = Path.Combine(projectDirectory, registrationFile);
        if (!File.Exists(registrationPath))
        {
            issues.Add(new GateIssue(
                "V3-PN-UC-009",
                patternNode.Id,
                registrationPath,
                null,
                $"Application 必須提供 {registrationFile} 集中註冊 UseCase。",
                "error"));
            return;
        }

        string registrationText = File.ReadAllText(registrationPath);
        if (!Regex.IsMatch(registrationText, $@"\b{Regex.Escape(registrationMethod)}\s*\("))
        {
            issues.Add(new GateIssue(
                "V3-PN-UC-009",
                patternNode.Id,
                registrationPath,
                1,
                $"{registrationFile} 必須提供 {registrationMethod} registration method。",
                "error"));
        }

        List<string> useCaseNames = new List<string>();
        foreach (string sourceFile in sourceFiles)
        {
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(sourceFile), path: sourceFile).GetCompilationUnitRoot();
            useCaseNames.AddRange(root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Select(classDeclaration => classDeclaration.Identifier.ValueText)
                .Where(className => className.EndsWith("UseCase", StringComparison.Ordinal)));
        }

        foreach (string useCaseName in useCaseNames.Distinct(StringComparer.Ordinal))
        {
            string transientPattern = $@"AddTransient\s*<\s*{Regex.Escape(useCaseName)}\s*>";
            if (!Regex.IsMatch(registrationText, transientPattern))
            {
                issues.Add(new GateIssue(
                    "V3-PN-UC-009",
                    patternNode.Id,
                    registrationPath,
                    1,
                    $"{useCaseName} 必須以 AddTransient<{useCaseName}> 註冊具體 UseCase class。",
                    "error"));
            }
        }
    }

    private void ValidateWinFormsCompositionRoot(
        PatternNodeDefinition patternNode,
        string projectDirectory,
        List<string> sourceFiles,
        List<GateIssue> issues)
    {
        string programPath = Path.Combine(projectDirectory, "Program.cs");
        if (!File.Exists(programPath))
        {
            return;
        }

        string programText = File.ReadAllText(programPath);
        GateCheckDefinition check = patternNode.Checks.First(candidate =>
            string.Equals(candidate.Checker, "dependency_injection", StringComparison.Ordinal));
        string applicationRegistrationMethod = check.ScalarParameters.GetValueOrDefault(
            "application_registration_method",
            "AddApplication");
        string infrastructureRegistrationMethod = check.ScalarParameters.GetValueOrDefault(
            "infrastructure_registration_method",
            "AddInfrastructure");
        string[] requiredCompositionCalls =
        {
            "ServiceCollection",
            applicationRegistrationMethod,
            infrastructureRegistrationMethod,
            "BuildServiceProvider",
            "GetRequiredService"
        };
        foreach (string requiredCall in requiredCompositionCalls)
        {
            if (!programText.Contains(requiredCall, StringComparison.Ordinal))
            {
                issues.Add(new GateIssue(
                    "V3-PN-UI-010",
                    patternNode.Id,
                    programPath,
                    1,
                    $"Program.cs composition root 必須包含 {requiredCall}。",
                    "error"));
            }
        }

        List<string> requiredTransientTypes = check.ListParameters.GetValueOrDefault(
            "required_transient_types",
            new List<string>());
        foreach (string transientType in requiredTransientTypes)
        {
            if (!Regex.IsMatch(programText, $@"AddTransient\s*<\s*{Regex.Escape(transientType)}\s*>"))
            {
                issues.Add(new GateIssue(
                    "V3-PN-UI-010",
                    patternNode.Id,
                    programPath,
                    1,
                    $"{transientType} 必須在 Program.cs 以 transient 註冊。",
                    "error"));
            }
        }
    }

    private void ValidateApplicationFeatureLayout(
        PatternNodeDefinition patternNode,
        string projectDirectory,
        string[] sourceFiles,
        List<GateIssue> issues)
    {
        foreach (string sourceFile in sourceFiles.Where(sourceFile => !IsGeneratedFile(projectDirectory, sourceFile)))
        {
            if (IsDependencyInjectionRegistrationFile(sourceFile))
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(projectDirectory, sourceFile).Replace('\\', '/');
            string[] segments = relativePath.Split('/');
            CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(File.ReadAllText(sourceFile), path: sourceFile).GetCompilationUnitRoot();
            bool isUseCaseFeature = segments.Length >= 2
                && segments[0].StartsWith("_", StringComparison.Ordinal)
                && segments[0].EndsWith("UseCase", StringComparison.Ordinal);
            bool isCommonSource = segments.Length >= 2
                && string.Equals(segments[0], "Common", StringComparison.Ordinal);
            bool isRepositoryContract = string.Equals(
                relativePath,
                "Repositories/IGameRepository.cs",
                StringComparison.Ordinal);
            string expectedNamespace = $"{Path.GetFileName(projectDirectory)}.{string.Join('.', segments.Take(segments.Length - 1))}";
            string? actualNamespace = root.DescendantNodes()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Select(namespaceDeclaration => namespaceDeclaration.Name.ToString())
                .FirstOrDefault();
            if (string.Equals(segments[0], "_GameState", StringComparison.Ordinal)
                || root.DescendantNodes().OfType<ClassDeclarationSyntax>().Any(classDeclaration =>
                    string.Equals(classDeclaration.Identifier.ValueText, "GameState", StringComparison.Ordinal)))
            {
                issues.Add(new GateIssue(
                    "V3-PN-UC-PRO-009",
                    patternNode.Id,
                    sourceFile,
                    1,
                    "Application 不得建立 _GameState 或 GameState；目前遊戲狀態由 Domain Game 擁有，並透過 IGameRepository 取得。",
                    "error"));
            }

            bool isAllowedRoot = isUseCaseFeature || isCommonSource || isRepositoryContract;
            if (!isAllowedRoot || !string.Equals(actualNamespace, expectedNamespace, StringComparison.Ordinal))
            {
                issues.Add(new GateIssue(
                    "V3-PN-UC-009",
                    patternNode.Id,
                    sourceFile,
                    1,
                    $"Application 格式錯誤：{relativePath}；只允許 UseCase feature、Common 或 Repositories/IGameRepository.cs，且必須使用對應 namespace。",
                    "error"));
                continue;
            }

            if (isRepositoryContract)
            {
                bool containsExpectedInterface = root.DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .Any(interfaceDeclaration => string.Equals(
                        interfaceDeclaration.Identifier.ValueText,
                        "IGameRepository",
                        StringComparison.Ordinal));
                if (!containsExpectedInterface)
                {
                    issues.Add(new GateIssue(
                        "V3-PN-UC-010",
                        patternNode.Id,
                        sourceFile,
                        1,
                        "Repositories/IGameRepository.cs 必須宣告 IGameRepository。",
                        "error"));
                }
            }

            foreach (ClassDeclarationSyntax useCase in root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(classDeclaration => classDeclaration.Identifier.ValueText.EndsWith("UseCase", StringComparison.Ordinal)))
            {
                string operation = useCase.Identifier.ValueText[..^"UseCase".Length];
                string expectedRelativePath = $"_{operation}UseCase/{operation}UseCase.cs";
                if (!string.Equals(relativePath, expectedRelativePath, StringComparison.Ordinal))
                {
                    AddIssue(
                        issues,
                        "V3-PN-UC-009",
                        patternNode.Id,
                        sourceFile,
                        useCase,
                        $"{operation}UseCase 必須位於 {expectedRelativePath}。");
                }

                bool injectsGameRepository = useCase.Members
                    .OfType<ConstructorDeclarationSyntax>()
                    .SelectMany(constructor => constructor.ParameterList.Parameters)
                    .Any(parameter => parameter.Type?.ToString().Split('.').Last() == "IGameRepository");
                if (!injectsGameRepository)
                {
                    AddIssue(
                        issues,
                        "V3-PN-UC-008",
                        patternNode.Id,
                        sourceFile,
                        useCase,
                        $"{operation}UseCase 必須透過 constructor injection 取得 IGameRepository。");
                }
            }

            bool isDtoDirectory = segments.Length == 3
                && string.Equals(segments[1], "Dto", StringComparison.Ordinal);
            bool isEnumDirectory = segments.Length == 3
                && string.Equals(segments[1], "Enums", StringComparison.Ordinal);
            foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                bool isDto = classDeclaration.Identifier.ValueText.EndsWith("DTO", StringComparison.Ordinal);
                if (isDtoDirectory && !isDto)
                {
                    AddIssue(
                        issues,
                        "V3-PN-UC-003",
                        patternNode.Id,
                        sourceFile,
                        classDeclaration,
                        $"Dto 資料夾內的 class {classDeclaration.Identifier.ValueText} 必須以 DTO 結尾。");
                }
                else if (isDto && !isDtoDirectory)
                {
                    AddIssue(
                        issues,
                        "V3-PN-UC-003",
                        patternNode.Id,
                        sourceFile,
                        classDeclaration,
                        $"DTO 型別 {classDeclaration.Identifier.ValueText} 必須位於所屬 UseCase 的 Dto 子資料夾。");
                }

                if (isEnumDirectory)
                {
                    AddIssue(
                        issues,
                        "V3-PN-UC-PRO-010",
                        patternNode.Id,
                        sourceFile,
                        classDeclaration,
                        $"Enums 資料夾只能放 enum，不得放 class {classDeclaration.Identifier.ValueText}。");
                }

                if (classDeclaration.Identifier.ValueText.EndsWith("Repository", StringComparison.Ordinal)
                    && !string.Equals(classDeclaration.Identifier.ValueText, "IGameRepository", StringComparison.Ordinal))
                {
                    AddIssue(
                        issues,
                        "V3-PN-UC-PRO-011",
                        patternNode.Id,
                        sourceFile,
                        classDeclaration,
                        $"Application 不得實作 {classDeclaration.Identifier.ValueText}；Repository 實作屬於 Infrastructure。");
                }
            }

            foreach (EnumDeclarationSyntax enumDeclaration in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
            {
                if (!isEnumDirectory)
                {
                    AddIssue(
                        issues,
                        "V3-PN-UC-PRO-010",
                        patternNode.Id,
                        sourceFile,
                        enumDeclaration,
                        $"UseCase enum {enumDeclaration.Identifier.ValueText} 必須位於該 UseCase 的 Enums 子資料夾。");
                }
            }
        }
    }

    private void ValidateWinFormsProgram(PatternNodeDefinition patternNode, string projectDirectory, List<GateIssue> issues)
    {
        string programPath = Path.Combine(projectDirectory, "Program.cs");
        if (!File.Exists(programPath))
        {
            issues.Add(new GateIssue("V3-PN-CS-ENTRY-001", patternNode.Id, programPath, null, "WinForms 專案必須提供 Program.cs。", "error"));
            return;
        }

        string sourceText = File.ReadAllText(programPath);
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(sourceText, path: programPath).GetCompilationUnitRoot();
        ClassDeclarationSyntax? program = root.Members
            .OfType<BaseNamespaceDeclarationSyntax>()
            .SelectMany(namespaceDeclaration => namespaceDeclaration.Members.OfType<ClassDeclarationSyntax>())
            .Concat(root.Members.OfType<ClassDeclarationSyntax>())
            .FirstOrDefault(classDeclaration => string.Equals(
                classDeclaration.Identifier.ValueText,
                "Program",
                StringComparison.Ordinal));
        MethodDeclarationSyntax? main = program?.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(method => string.Equals(method.Identifier.ValueText, "Main", StringComparison.Ordinal));
        bool hasValidProgramClass = program is not null
            && program.Modifiers.Any(SyntaxKind.InternalKeyword)
            && program.Modifiers.Any(SyntaxKind.StaticKeyword);
        bool hasValidMain = main is not null
            && main.Modifiers.Any(SyntaxKind.StaticKeyword)
            && string.Equals(main.ReturnType.ToString(), "void", StringComparison.Ordinal)
            && main.ParameterList.Parameters.Count == 0
            && main.AttributeLists.SelectMany(list => list.Attributes).Any(attribute =>
                attribute.Name.ToString() is "STAThread" or "STAThreadAttribute");
        if (!hasValidProgramClass || !hasValidMain || root.Members.OfType<GlobalStatementSyntax>().Any())
        {
            issues.Add(new GateIssue(
                "V3-PN-CS-ENTRY-001",
                patternNode.Id,
                programPath,
                1,
                "Program.cs 必須使用 internal static class Program、[STAThread] 與 static void Main()，不得使用頂層陳述式。",
                "error"));
        }
    }

    private bool HasChecker(PatternNodeDefinition patternNode, string checker)
    {
        return patternNode.Checks.Any(candidate => string.Equals(candidate.Checker, checker, StringComparison.Ordinal));
    }

    private bool IsDependencyInjectionRegistrationFile(string sourceFile)
    {
        return string.Equals(Path.GetFileName(sourceFile), "DependencyInjection.cs", StringComparison.Ordinal);
    }

    private bool IsGeneratedFile(string projectDirectory, string sourceFile)
    {
        string relativePath = Path.GetRelativePath(projectDirectory, sourceFile).Replace('\\', '/');
        return relativePath.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || sourceFile.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
            || sourceFile.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || sourceFile.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsWinFormsType(string typeName)
    {
        string simpleTypeName = typeName.Split('.').Last();
        return simpleTypeName is "Form" or "Control" or "Button" or "Timer" or "Label" or "Panel" or "PictureBox" or "TextBox" or "ComboBox" or "ListBox" or "DataGridView";
    }

    private bool IsPascalCase(string value)
    {
        return Regex.IsMatch(value, "^[A-Z][A-Za-z0-9]*$");
    }

    private bool IsCamelCase(string value)
    {
        return Regex.IsMatch(value, "^[a-z][A-Za-z0-9]*$");
    }

    private void AddIssue(
        List<GateIssue> issues,
        string ruleId,
        string node,
        string file,
        SyntaxNode syntaxNode,
        string message,
        string severity = "error")
    {
        FileLinePositionSpan lineSpan = syntaxNode.GetLocation().GetLineSpan();
        issues.Add(new GateIssue(ruleId, node, file, lineSpan.StartLinePosition.Line + 1, message.TrimEnd(), severity));
    }
}
