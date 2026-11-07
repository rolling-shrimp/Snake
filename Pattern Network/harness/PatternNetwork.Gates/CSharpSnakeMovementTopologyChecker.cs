namespace PatternNetwork.Gates;

public class CSharpSnakeMovementTopologyChecker : IGateChecker
{
    private string _projectRoot;

    public CSharpSnakeMovementTopologyChecker(string projectRoot)
    {
        _projectRoot = Path.GetFullPath(projectRoot);
    }

    public string Id => "csharp_snake_movement_topology";

    public GateCheckResult Check(SpecificationInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        string snakePath = Path.Combine(_projectRoot, "Core", "Snake.Domain", "Snake.cs");
        string useCasePath = Path.Combine(
            _projectRoot,
            "Core",
            "Snake.Application",
            "_MoveSnakeUseCase",
            "MoveSnakeUseCase.cs");
        string testsPath = Path.Combine(_projectRoot, "Test", "Snake.Test", "MoveSnakeUseCaseTests.cs");
        List<GateIssue> issues = new List<GateIssue>();
        RequireTokens(
            snakePath,
            new List<string>
            {
                "IsContinuous(",
                "ReplaceBodyCells("
            },
            issues);
        RequireTokens(
            useCasePath,
            new List<string>
            {
                "class MoveSnakeUseCase",
                "void MoveSnake(",
                "ReplaceBodyCells("
            },
            issues);
        RequireTokens(
            testsPath,
            new List<string>
            {
                "MoveSnake_MovesExactlyOneCell",
                "MoveSnake_PreservesOrderedUniqueContinuousBody"
            },
            issues);

        if (File.Exists(snakePath))
        {
            string snakeSource = File.ReadAllText(snakePath);
            List<string> forbiddenMutators = new List<string>
            {
                "public void Move(",
                "class MoveSnakeUseCase"
            };
            foreach (string mutator in forbiddenMutators)
            {
                int index = snakeSource.IndexOf(mutator, StringComparison.Ordinal);
                if (index >= 0)
                {
                    issues.Add(CreateIssue(
                        snakePath,
                        GetLine(snakeSource, index),
                        $"Domain 不得重複擁有完整 MoveSnake Use Case：{mutator}。"));
                }
            }
        }

        if (File.Exists(useCasePath))
        {
            string useCaseSource = File.ReadAllText(useCasePath);
            foreach (string forbiddenToken in new[] { "DateTime", "Stopwatch", "while (", "for (" })
            {
                int index = useCaseSource.IndexOf(forbiddenToken, StringComparison.Ordinal);
                if (index >= 0)
                {
                    issues.Add(CreateIssue(
                        useCasePath,
                        GetLine(useCaseSource, index),
                        $"MoveSnakeUseCase 一次呼叫不得使用累積時間或迴圈補跑：{forbiddenToken}。"));
                }
            }
        }

        return new GateCheckResult(
            Id,
            VerificationMode.Audit,
            issues.Count == 0 ? VerificationStatus.Pass : VerificationStatus.Fail,
            new List<string>(),
            new List<string>
            {
                "V3-SCN-MOVE-SNAKE-RIGHT-ONCE",
                "V3-SCN-MOVE-SNAKE-NO-CATCH-UP",
                "V3-SCN-MOVE-SNAKE-PRESERVES-TOPOLOGY"
            },
            new List<string>
            {
                $"aggregate:{snakePath}",
                $"use-case:{useCasePath}",
                $"behavioral-tests:{testsPath}"
            },
            issues);
    }

    private void RequireTokens(string file, List<string> tokens, List<GateIssue> issues)
    {
        if (!File.Exists(file))
        {
            issues.Add(CreateIssue(file, null, "找不到拓樸檢查所需檔案。"));
            return;
        }

        string source = File.ReadAllText(file);
        foreach (string token in tokens)
        {
            if (!source.Contains(token, StringComparison.Ordinal))
            {
                issues.Add(CreateIssue(file, null, $"缺少蛇身拓樸證據：{token}。"));
            }
        }
    }

    private GateIssue CreateIssue(string file, int? line, string message)
    {
        return new GateIssue(
            "V3-CODE-SNAKE-MOVEMENT-TOPOLOGY",
            "SnakeMovement",
            file,
            line,
            message,
            "warning");
    }

    private int GetLine(string source, int index)
    {
        return source.Take(index).Count(character => character == '\n') + 1;
    }
}
