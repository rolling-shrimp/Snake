using System.Drawing;
using Snake;

namespace Snake.Tests;

public class GameTestDriver
{
    public SnakeGame CreateGame(int level, GamePhase phase, int snakeLength = 3, Dictionary<Point, GameObjectType>? objects = null, int shieldCount = 0, Direction? direction = null, bool firstDoorOpen = true, bool secondDoorOpen = false, long nextMoveAtMilliseconds = 250, long nextDoorAtMilliseconds = 6000)
    {
        SnakeGame game = new SnakeGame();
        game.SetTestScenario(level, phase, snakeLength, objects: objects, shieldCount: shieldCount, direction: direction ?? Direction.Right, firstDoorOpen: firstDoorOpen, secondDoorOpen: secondDoorOpen, nextMoveAtMilliseconds: nextMoveAtMilliseconds, nextDoorAtMilliseconds: nextDoorAtMilliseconds);
        return game;
    }

    public SnakeGame CreateGame(int level, GamePhase phase, List<Point> snake, Direction direction, Dictionary<Point, GameObjectType>? objects = null, int shieldCount = 0, int shieldPickupsSpawned = 0, int shieldPickupsCollected = 0, bool firstDoorOpen = true, bool secondDoorOpen = false, bool firstDoorPendingClose = false, bool secondDoorPendingClose = false, int? lastCompletedLevel = null, long nextMoveAtMilliseconds = 250, long nextDoorAtMilliseconds = 6000)
    {
        SnakeGame game = new SnakeGame();
        game.SetTestScenario(level, phase, snake.Count, snake, objects, shieldCount, shieldPickupsSpawned, shieldPickupsCollected, direction, firstDoorOpen, secondDoorOpen, firstDoorPendingClose, secondDoorPendingClose, lastCompletedLevel, nextMoveAtMilliseconds: nextMoveAtMilliseconds, nextDoorAtMilliseconds: nextDoorAtMilliseconds);
        return game;
    }

    public List<Point> SnakeOnSafePath(SnakeGame game, int length)
    {
        return game.SafePath.Take(length).Reverse().ToList();
    }

    public Direction DirectionAlongSafePath(SnakeGame game, int length)
    {
        Point tailward = game.SafePath[length - 1];
        Point next = game.SafePath[length];
        if (next.X > tailward.X)
        {
            return Direction.Right;
        }

        if (next.X < tailward.X)
        {
            return Direction.Left;
        }

        if (next.Y > tailward.Y)
        {
            return Direction.Down;
        }

        return Direction.Up;
    }

    public Point NextPoint(Point point, Direction direction, bool wrap = false)
    {
        Point next = direction switch
        {
            Direction.Up => new Point(point.X, point.Y - 1),
            Direction.Down => new Point(point.X, point.Y + 1),
            Direction.Left => new Point(point.X - 1, point.Y),
            _ => new Point(point.X + 1, point.Y)
        };

        if (wrap)
        {
            next = new Point((next.X + 10) % 10, (next.Y + 10) % 10);
        }

        return next;
    }

    public void Advance(SnakeGame game, int milliseconds = 250)
    {
        game.SetTestElapsedMilliseconds(milliseconds);
        if (game.Level == 1)
        {
            game.AdvanceFirstLevelStep();
        }
        else if (game.Level == 2)
        {
            game.AdvanceSecondLevelStep();
        }
        else
        {
            game.AdvanceThirdLevelStep();
        }
    }

    public void AssertObjectsAreLegal(SnakeGame game)
    {
        Assert.All(game.Objects.Keys, position =>
        {
            Assert.InRange(position.X, 0, 9);
            Assert.InRange(position.Y, 0, 9);
            Assert.DoesNotContain(position, game.Snake);
            Assert.DoesNotContain(position, game.Walls);
            Assert.DoesNotContain(position, game.Doors);
        });

        Assert.Equal(game.Objects.Count, game.Objects.Values.Distinct().Count());
    }
}
