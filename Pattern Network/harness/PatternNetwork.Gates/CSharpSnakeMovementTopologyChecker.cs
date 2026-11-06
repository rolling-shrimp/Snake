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
        string snakePath = Path.Combine(_projectRoot, "Snake.Entities", "Snake.cs");
        string boardPath = Path.Combine(_projectRoot, "Snake.Entities", "Board.cs");
        string servicePath = Path.Combine(_projectRoot, "Snake.UseCases", "SnakeGameApplicationService.cs");
        string testsPath = Path.Combine(_projectRoot, "Snake.Test", "SnakeMovementPlanTests.cs");
        List<GateIssue> issues = new List<GateIssue>();
        RequireTokens(
            snakePath,
            new List<string>
            {
                "PlanMovement(",
                "CommitMovement(",
                "ResetSafely(",
                "Take(_bodyCells.Count - 1)",
                "growthDelta == 2"
            },
            issues);
        RequireTokens(
            boardPath,
            new List<string>
            {
                "IsContinuous(",
                "GetTailExtensionCandidates(",
                "FindSafeBody("
            },
            issues);
        RequireTokens(
            servicePath,
            new List<string>
            {
                "FoodType.Ordinary => 1",
                "FoodType.Special => 2",
                "PlanMovement(",
                "CommitMovement("
            },
            issues);
        RequireTokens(
            testsPath,
            new List<string>
            {
                "PlanMovement_ExcludesDepartingTailFromNormalMoveCollisionCells",
                "PlanMovement_SelectsFirstLegalSpecialFoodTailCandidate",
                "CommitMovement_RejectsAPlanWhoseBodyIsNotContinuous"
            },
            issues);

        if (File.Exists(snakePath))
        {
            string snakeSource = File.ReadAllText(snakePath);
            List<string> forbiddenMutators = new List<string>
            {
                "public void MoveTo(",
                "public void Grow(",
                "public void Reset("
            };
            foreach (string mutator in forbiddenMutators)
            {
                int index = snakeSource.IndexOf(mutator, StringComparison.Ordinal);
                if (index >= 0)
                {
                    issues.Add(CreateIssue(
                        snakePath,
                        GetLine(snakeSource, index),
                        $"舊蛇身 mutation 會繞過移動計畫契約：{mutator}。"));
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
                "V3-SCN-TICK-MOVE-INTO-LEAVING-TAIL",
                "V3-SCN-TICK-ORDINARY-FOOD-GROWTH",
                "V3-SCN-TICK-SPECIAL-FOOD-CANDIDATE-ORDER",
                "V3-SCN-TICK-SPECIAL-FOOD-ATOMIC-ROLLBACK"
            },
            new List<string>
            {
                $"aggregate:{snakePath}",
                $"board:{boardPath}",
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
