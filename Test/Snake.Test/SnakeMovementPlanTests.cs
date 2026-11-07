using Snake.Domain;

namespace Snake.Test;

public class SnakeMovementPlanTests
{
    [Fact]
    public void PlanMovement_ExcludesDepartingTailFromNormalMoveCollisionCells()
    {
        Board board = new Board();
        Snake.Domain.Snake snake = new Snake.Domain.Snake(
            new List<GridPosition>
            {
                new GridPosition(1, 1),
                new GridPosition(1, 2),
                new GridPosition(2, 2),
                new GridPosition(2, 1)
            });

        SnakeMovementPlanResult result = snake.PlanMovement(
            new GridPosition(2, 1),
            0,
            board,
            BoundaryPolicy.Wrap,
            new List<GridPosition>());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Plan);
        Assert.True(result.Plan.TailWillLeave);
        Assert.DoesNotContain(new GridPosition(2, 1), result.Plan.SelfCollisionCells);
        Assert.Equal(
            new List<GridPosition>
            {
                new GridPosition(2, 1),
                new GridPosition(1, 1),
                new GridPosition(1, 2),
                new GridPosition(2, 2)
            },
            result.Plan.ResultingBodyCells);
    }

    [Fact]
    public void PlanMovement_SelectsFirstLegalSpecialFoodTailCandidate()
    {
        Board board = new Board();
        Snake.Domain.Snake snake = new Snake.Domain.Snake(
            new List<GridPosition>
            {
                new GridPosition(4, 4),
                new GridPosition(3, 4),
                new GridPosition(2, 4)
            });

        SnakeMovementPlanResult result = snake.PlanMovement(
            new GridPosition(5, 4),
            2,
            board,
            BoundaryPolicy.Wrap,
            new List<GridPosition>
            {
                new GridPosition(1, 4)
            });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Plan);
        Assert.Equal(new GridPosition(2, 5), result.Plan.ResultingBodyCells[^1]);
        Assert.Equal(5, result.Plan.ResultingBodyCells.Count);
        Assert.True(board.IsContinuous(result.Plan.ResultingBodyCells, BoundaryPolicy.Wrap));
    }

    [Fact]
    public void PlanMovement_ReturnsFailureWhenSpecialFoodHasNoLegalTailCandidate()
    {
        Board board = new Board();
        Snake.Domain.Snake snake = new Snake.Domain.Snake(
            new List<GridPosition>
            {
                new GridPosition(4, 4),
                new GridPosition(3, 4),
                new GridPosition(2, 4)
            });

        SnakeMovementPlanResult result = snake.PlanMovement(
            new GridPosition(5, 4),
            2,
            board,
            BoundaryPolicy.Wrap,
            new List<GridPosition>
            {
                new GridPosition(1, 4),
                new GridPosition(2, 5),
                new GridPosition(2, 3)
            });

        Assert.False(result.IsSuccess);
        Assert.Equal("SnakeTopologyUnavailable", result.FailureCode);
        Assert.Null(result.Plan);
        Assert.Equal(3, snake.Length);
    }

    [Fact]
    public void ReplaceBodyCells_RejectsAPlanWhoseBodyIsNotContinuous()
    {
        Board board = new Board();
        Snake.Domain.Snake snake = new Snake.Domain.Snake(
            new List<GridPosition>
            {
                new GridPosition(3, 1),
                new GridPosition(2, 1),
                new GridPosition(1, 1)
            });
        SnakeMovementPlan invalidPlan = new SnakeMovementPlan(
            new GridPosition(4, 1),
            0,
            true,
            new List<GridPosition>(),
            new List<GridPosition>
            {
                new GridPosition(4, 1),
                new GridPosition(2, 1),
                new GridPosition(1, 1)
            });

        Assert.Throws<InvalidOperationException>(() => snake.ReplaceBodyCells(
            invalidPlan,
            board,
            BoundaryPolicy.Wrap));
    }
}
