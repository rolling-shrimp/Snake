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
            issues.Add(new GateIssue("PN-SOURCE-READ", patternNode.Id, projectDirectory, null, $"列舉 C# 原始碼失敗：{exception.Message}", "error"));
            return issues;
        }

        Array.Sort(sourceFiles, StringComparer.Ordinal);
        foreach (string sourceFile in sourceFiles)
        {
            if (IsGeneratedFile(projectDirectory, sourceFile))
            {
                continue;
            }

            InspectFile(patternNode, sourceFile, issues);
        }

        if (HasChecker(patternNode, "top_level_program"))
        {
            ValidateTopLevelProgram(patternNode, projectDirectory, issues);
        }

        return issues;
    }

    private void InspectFile(PatternNodeDefinition patternNode, string sourceFile, List<GateIssue> issues)
    {
        try
        {
            string sourceText = File.ReadAllText(sourceFile);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: sourceFile);
            CompilationUnitSyntax root = syntaxTree.GetCompilationUnitRoot();
            foreach (Diagnostic diagnostic in syntaxTree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();
                issues.Add(new GateIssue("PN-CS-SYNTAX", patternNode.Id, sourceFile, lineSpan.StartLinePosition.Line + 1, diagnostic.GetMessage(), "error"));
            }

            if (HasChecker(patternNode, "csharp_global_rules"))
            {
                ValidateGlobalRules(patternNode, sourceFile, root, issues);
            }

            ValidateForbiddenNamespaces(patternNode, sourceFile, root, issues);
            if (HasChecker(patternNode, "entity_boundaries"))
            {
                ValidateEntityBoundaries(patternNode, sourceFile, root, issues);
            }

            if (HasChecker(patternNode, "use_case_contracts"))
            {
                ValidateUseCaseContracts(patternNode, sourceFile, root, issues);
            }

            if (HasChecker(patternNode, "winforms_boundaries"))
            {
                ValidateWinFormsBoundaries(patternNode, sourceFile, root, issues);
            }
        }
        catch (Exception exception)
        {
            issues.Add(new GateIssue("PN-SOURCE-READ", patternNode.Id, sourceFile, null, $"讀取或分析 C# 原始碼失敗：{exception.Message}", "error"));
        }
    }

    private void ValidateGlobalRules(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
        foreach (RecordDeclarationSyntax record in root.DescendantNodes().OfType<RecordDeclarationSyntax>())
        {
            AddIssue(issues, "PN-CS-CLASS-002", patternNode.Id, sourceFile, record, "手寫型別不得使用 record。 ");
        }

        List<ClassDeclarationSyntax> classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        if (classes.Count > 1)
        {
            AddIssue(issues, "PN-CS-FILE-001", patternNode.Id, sourceFile, classes[1], "一個 .cs 檔案只能包含一個類別。 ");
        }

        foreach (ClassDeclarationSyntax classDeclaration in classes)
        {
            ValidateClassDeclaration(patternNode, sourceFile, classDeclaration, issues);
        }

        if (classes.Count == 1 && !string.Equals(Path.GetFileNameWithoutExtension(sourceFile), classes[0].Identifier.ValueText, StringComparison.Ordinal))
        {
            AddIssue(issues, "PN-CS-FILE-001", patternNode.Id, sourceFile, classes[0], $"類別 {classes[0].Identifier.ValueText} 必須放在同名檔案。 ");
        }

        foreach (FieldDeclarationSyntax field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            ValidateField(patternNode, sourceFile, field, issues);
        }

        ValidateConstructorDependencyAssignments(patternNode, sourceFile, root, issues);

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (method.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                AddIssue(issues, "PN-CS-METHOD-001", patternNode.Id, sourceFile, method, $"方法 {method.Identifier.ValueText} 不得宣告 static。 ");
            }

            if (!IsPascalCase(method.Identifier.ValueText))
            {
                AddIssue(issues, "PN-CS-NAME-001", patternNode.Id, sourceFile, method, $"方法 {method.Identifier.ValueText} 必須使用 PascalCase。 ");
            }
        }

        foreach (PropertyDeclarationSyntax property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (!IsPascalCase(property.Identifier.ValueText))
            {
                AddIssue(issues, "PN-CS-NAME-001", patternNode.Id, sourceFile, property, $"屬性 {property.Identifier.ValueText} 必須使用 PascalCase。 ");
            }
        }

        foreach (EnumDeclarationSyntax enumDeclaration in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            if (!IsPascalCase(enumDeclaration.Identifier.ValueText))
            {
                AddIssue(issues, "PN-CS-NAME-001", patternNode.Id, sourceFile, enumDeclaration, $"列舉 {enumDeclaration.Identifier.ValueText} 必須使用 PascalCase。 ");
            }
        }

        foreach (ParameterSyntax parameter in root.DescendantNodes().OfType<ParameterSyntax>())
        {
            if (!IsCamelCase(parameter.Identifier.ValueText))
            {
                AddIssue(issues, "PN-CS-NAME-001", patternNode.Id, sourceFile, parameter, $"參數 {parameter.Identifier.ValueText} 必須使用 camelCase。 ");
            }
        }

        foreach (VariableDeclaratorSyntax variable in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (variable.Parent?.Parent is LocalDeclarationStatementSyntax && !IsCamelCase(variable.Identifier.ValueText))
            {
                AddIssue(issues, "PN-CS-NAME-001", patternNode.Id, sourceFile, variable, $"區域變數 {variable.Identifier.ValueText} 必須使用 camelCase。 ");
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
                    AddIssue(issues, "PN-CS-DI-001", patternNode.Id, sourceFile, assignment, $"建構式參數 {parameterReference.Identifier.ValueText} 必須保存至 {expectedFieldName}，不得保存至 {fieldName}。 ");
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
        if (!classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            AddIssue(issues, "PN-CS-CLASS-001", patternNode.Id, sourceFile, classDeclaration, $"類別 {classDeclaration.Identifier.ValueText} 必須宣告 public。 ");
        }

        foreach (SyntaxKind forbiddenModifier in new[] { SyntaxKind.SealedKeyword, SyntaxKind.AbstractKeyword, SyntaxKind.PartialKeyword, SyntaxKind.StaticKeyword })
        {
            if (classDeclaration.Modifiers.Any(forbiddenModifier))
            {
                string modifierText = SyntaxFacts.GetText(forbiddenModifier);
                AddIssue(issues, "PN-CS-CLASS-001", patternNode.Id, sourceFile, classDeclaration, $"手寫類別 {classDeclaration.Identifier.ValueText} 不得宣告 {modifierText}。 ");
            }
        }

        if (!IsPascalCase(classDeclaration.Identifier.ValueText))
        {
            AddIssue(issues, "PN-CS-NAME-001", patternNode.Id, sourceFile, classDeclaration, $"類別 {classDeclaration.Identifier.ValueText} 必須使用 PascalCase。 ");
        }
    }

    private void ValidateField(PatternNodeDefinition patternNode, string sourceFile, FieldDeclarationSyntax field, List<GateIssue> issues)
    {
        bool isConst = field.Modifiers.Any(SyntaxKind.ConstKeyword);
        if (!field.Modifiers.Any(SyntaxKind.PrivateKeyword))
        {
            AddIssue(issues, "PN-CS-FIELD-001", patternNode.Id, sourceFile, field, "欄位必須宣告 private。 ");
        }

        if (field.Modifiers.Any(SyntaxKind.StaticKeyword) || field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            AddIssue(issues, "PN-CS-FIELD-002", patternNode.Id, sourceFile, field, "欄位不得宣告 static 或 readonly。 ");
        }

        foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
        {
            if (isConst)
            {
                if (!Regex.IsMatch(variable.Identifier.ValueText, "^[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*$"))
                {
                    AddIssue(issues, "PN-CS-CONST-001", patternNode.Id, sourceFile, variable, $"常數 {variable.Identifier.ValueText} 必須使用 UPPER_SNAKE_CASE。 ");
                }
            }
            else if (!Regex.IsMatch(variable.Identifier.ValueText, "^_[a-z][A-Za-z0-9]*$"))
            {
                AddIssue(issues, "PN-CS-FIELD-001", patternNode.Id, sourceFile, variable, $"欄位 {variable.Identifier.ValueText} 必須使用 _camelCase。 ");
            }
        }
    }

    private void ValidateControlFlowBraces(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
        foreach (IfStatementSyntax statement in root.DescendantNodes().OfType<IfStatementSyntax>())
        {
            if (statement.Statement is not BlockSyntax)
            {
                AddIssue(issues, "PN-CS-FLOW-001", patternNode.Id, sourceFile, statement.Statement, "if 或 else if 主體必須使用大括號。 ");
            }

            if (statement.Else != null && statement.Else.Statement is not BlockSyntax && statement.Else.Statement is not IfStatementSyntax)
            {
                AddIssue(issues, "PN-CS-FLOW-001", patternNode.Id, sourceFile, statement.Else.Statement, "else 主體必須使用大括號。 ");
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
            AddIssue(issues, "PN-CS-FLOW-001", patternNode.Id, sourceFile, body, "迴圈主體必須使用大括號。 ");
        }
    }

    private void ValidateForbiddenNamespaces(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
        GateCheckDefinition? check = patternNode.Checks.FirstOrDefault(candidate => string.Equals(candidate.Checker, "forbidden_namespaces", StringComparison.Ordinal));
        if (check == null || !check.ListParameters.TryGetValue("namespaces", out List<string>? forbiddenNamespaces))
        {
            return;
        }

        string ruleId = patternNode.Id == "entities" ? "PN-ENT-PRO-002" : "PN-UC-PRO-002";
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

    private void ValidateEntityBoundaries(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
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
                    AddIssue(issues, "PN-ENT-PRO-004", patternNode.Id, sourceFile, accessor, $"領域屬性 {property.Identifier.ValueText} 不得提供 public {accessor.Keyword.ValueText}。 ");
                }
            }
        }

        foreach (MemberAccessExpressionSyntax memberAccess in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            string expression = memberAccess.ToString();
            if (expression is "DateTime.Now" or "DateTime.UtcNow" or "Guid.NewGuid" || expression.StartsWith("Environment.", StringComparison.Ordinal))
            {
                AddIssue(issues, "PN-ENT-PRO-003", patternNode.Id, sourceFile, memberAccess, $"Entities 不得直接取得非確定性來源：{expression}。 ");
            }
        }

        foreach (ObjectCreationExpressionSyntax objectCreation in root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>())
        {
            if (objectCreation.Type.ToString() is "Random" or "System.Random")
            {
                AddIssue(issues, "PN-ENT-PRO-003", patternNode.Id, sourceFile, objectCreation, "Entities 不得自行建立 Random。 ");
            }
        }
    }

    private void ValidateUseCaseContracts(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
        foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            if (classDeclaration.Identifier.ValueText.EndsWith("UseCase", StringComparison.Ordinal))
            {
                List<MethodDeclarationSyntax> executeMethods = classDeclaration.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword) && method.Identifier.ValueText == "Execute")
                    .ToList();
                if (executeMethods.Count != 1)
                {
                    AddIssue(issues, "PN-UC-002", patternNode.Id, sourceFile, classDeclaration, $"{classDeclaration.Identifier.ValueText} 必須有且只有一個 public Execute。 ");
                    continue;
                }

                MethodDeclarationSyntax executeMethod = executeMethods[0];
                if (executeMethod.ParameterList.Parameters.Count != 1)
                {
                    AddIssue(issues, "PN-UC-002", patternNode.Id, sourceFile, executeMethod, "Execute 必須接受一個 Command 或 Query。 ");
                }
                else
                {
                    string inputType = executeMethod.ParameterList.Parameters[0].Type?.ToString() ?? string.Empty;
                    if (!inputType.EndsWith("Command", StringComparison.Ordinal) && !inputType.EndsWith("Query", StringComparison.Ordinal))
                    {
                        AddIssue(issues, "PN-UC-PRO-003", patternNode.Id, sourceFile, executeMethod.ParameterList.Parameters[0], $"Execute 輸入型別 {inputType} 必須以 Command 或 Query 結尾。 ");
                    }
                }

                if (!executeMethod.ReturnType.ToString().EndsWith("Result", StringComparison.Ordinal))
                {
                    AddIssue(issues, "PN-UC-002", patternNode.Id, sourceFile, executeMethod.ReturnType, "Execute 回傳型別必須以 Result 結尾。 ");
                }
            }

            if (classDeclaration.Identifier.ValueText.EndsWith("Result", StringComparison.Ordinal))
            {
                ValidateResultContract(patternNode, sourceFile, classDeclaration, issues);
            }
        }
    }

    private void ValidateResultContract(PatternNodeDefinition patternNode, string sourceFile, ClassDeclarationSyntax resultClass, List<GateIssue> issues)
    {
        Dictionary<string, string> requiredProperties = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["IsSuccess"] = "bool",
            ["FailureCode"] = "string?",
            ["FailureMessage"] = "string?"
        };
        Dictionary<string, PropertyDeclarationSyntax> actualProperties = resultClass.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(property => property.Modifiers.Any(SyntaxKind.PublicKeyword))
            .ToDictionary(property => property.Identifier.ValueText, StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> requiredProperty in requiredProperties)
        {
            if (!actualProperties.TryGetValue(requiredProperty.Key, out PropertyDeclarationSyntax? actualProperty) || !string.Equals(actualProperty.Type.ToString(), requiredProperty.Value, StringComparison.Ordinal))
            {
                AddIssue(issues, "PN-UC-003", patternNode.Id, sourceFile, resultClass, $"Result 必須提供 {requiredProperty.Value} {requiredProperty.Key}。 ");
            }
        }
    }

    private void ValidateWinFormsBoundaries(PatternNodeDefinition patternNode, string sourceFile, CompilationUnitSyntax root, List<GateIssue> issues)
    {
        foreach (ClassDeclarationSyntax presenter in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(type => type.Identifier.ValueText.EndsWith("Presenter", StringComparison.Ordinal)))
        {
            foreach (BaseTypeSyntax baseType in presenter.BaseList?.Types ?? new SeparatedSyntaxList<BaseTypeSyntax>())
            {
                if (IsWinFormsType(baseType.Type.ToString()))
                {
                    AddIssue(issues, "PN-UI-PRO-002", patternNode.Id, sourceFile, baseType, "Presenter 不得繼承 WinForms 型別。 ");
                }
            }

            foreach (FieldDeclarationSyntax field in presenter.Members.OfType<FieldDeclarationSyntax>())
            {
                if (IsWinFormsType(field.Declaration.Type.ToString()))
                {
                    AddIssue(issues, "PN-UI-PRO-002", patternNode.Id, sourceFile, field, "Presenter 不得保存 WinForms 控制項。 ");
                }
            }

            foreach (ConstructorDeclarationSyntax constructor in presenter.Members.OfType<ConstructorDeclarationSyntax>())
            {
                foreach (ParameterSyntax parameter in constructor.ParameterList.Parameters)
                {
                    if (IsWinFormsType(parameter.Type?.ToString() ?? string.Empty))
                    {
                        AddIssue(issues, "PN-UI-PRO-002", patternNode.Id, sourceFile, parameter, "Presenter 建構式不得接收 WinForms 控制項。 ");
                    }
                }
            }
        }
    }

    private void ValidateTopLevelProgram(PatternNodeDefinition patternNode, string projectDirectory, List<GateIssue> issues)
    {
        string programPath = Path.Combine(projectDirectory, "Program.cs");
        if (!File.Exists(programPath))
        {
            issues.Add(new GateIssue("PN-CS-ENTRY-001", patternNode.Id, programPath, null, "WinForms 專案必須提供頂層 Program.cs。", "error"));
            return;
        }

        string sourceText = File.ReadAllText(programPath);
        CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(sourceText, path: programPath).GetCompilationUnitRoot();
        if (!root.Members.OfType<GlobalStatementSyntax>().Any())
        {
            issues.Add(new GateIssue("PN-CS-ENTRY-001", patternNode.Id, programPath, 1, "Program.cs 必須使用頂層陳述式。", "error"));
        }
    }

    private bool HasChecker(PatternNodeDefinition patternNode, string checker)
    {
        return patternNode.Checks.Any(candidate => string.Equals(candidate.Checker, checker, StringComparison.Ordinal));
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

    private void AddIssue(List<GateIssue> issues, string ruleId, string node, string file, SyntaxNode syntaxNode, string message)
    {
        FileLinePositionSpan lineSpan = syntaxNode.GetLocation().GetLineSpan();
        issues.Add(new GateIssue(ruleId, node, file, lineSpan.StartLinePosition.Line + 1, message.TrimEnd(), "error"));
    }
}
