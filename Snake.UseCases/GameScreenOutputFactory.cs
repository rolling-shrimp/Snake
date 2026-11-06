using Snake.Entities;

namespace Snake.UseCases;

public class GameScreenOutputFactory
{
    public GameScreenOutput Create(SnakeGameSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        GameScreenKind screenKind = GetScreenKind(snapshot);
        List<GridPositionDTO> snakeBody = snapshot.Snake.BodyCells
            .Select(ToPosition)
            .ToList();
        List<BoardObjectOutput> objects = CreateObjects(snapshot);
        List<GridPositionDTO> wallCells = snapshot.DoorAndMaze.WallCells
            .Select(ToPosition)
            .ToList();
        List<DoorOutput> doors = snapshot.DoorAndMaze.Doors
            .Select(door => new DoorOutput(
                door.DoorId.Value.ToString(),
                ToPosition(door.Position),
                MapDoorState(door.Phase)))
            .ToList();
        List<BoardCellOutput> boardCells = CreateBoardCells(snapshot, objects);
        LevelStatusOutput levelStatus = new LevelStatusOutput(
            (int)snapshot.Level.Number,
            snapshot.Snake.Length,
            snapshot.Level.TargetLength,
            snapshot.Level.GetProgressPercent(snapshot.Snake.Length),
            snapshot.Shield.RemainingCharges);
        return new GameScreenOutput(
            screenKind,
            boardCells,
            snakeBody,
            objects,
            wallCells,
            doors,
            levelStatus,
            CreateRuleHints(snapshot.Level.Number),
            CreateResultCard(snapshot),
            CreateAvailableActions(snapshot.Game.Phase));
    }

    private GameScreenKind GetScreenKind(SnakeGameSnapshot snapshot)
    {
        if (snapshot.Game.Phase == GamePhase.GameOver)
        {
            return GameScreenKind.GameOver;
        }

        if (snapshot.Game.Phase == GamePhase.Completed)
        {
            return GameScreenKind.GameCompleted;
        }

        if (snapshot.Game.Phase == GamePhase.Ready)
        {
            return snapshot.Level.Number switch
            {
                LevelNumber.First => GameScreenKind.FirstLevelReady,
                LevelNumber.Second => GameScreenKind.SecondLevelReady,
                LevelNumber.Third => GameScreenKind.ThirdLevelReady,
                _ => throw new InvalidOperationException("目前關卡無法對應待開始畫面。")
            };
        }

        if (snapshot.Game.Phase == GamePhase.Playing)
        {
            return snapshot.Level.Number switch
            {
                LevelNumber.First => GameScreenKind.FirstLevelPlaying,
                LevelNumber.Second => GameScreenKind.SecondLevelPlaying,
                LevelNumber.Third => GameScreenKind.ThirdLevelPlaying,
                _ => throw new InvalidOperationException("目前關卡無法對應遊玩畫面。")
            };
        }

        throw new InvalidOperationException("目前 Game phase 無法對應畫面。");
    }

    private List<BoardObjectOutput> CreateObjects(SnakeGameSnapshot snapshot)
    {
        List<BoardObjectOutput> objects = new List<BoardObjectOutput>();
        foreach (FoodItem item in snapshot.Food.ActiveItems.OrderBy(item => item.Type))
        {
            objects.Add(new BoardObjectOutput(
                item.FoodItemId.Value.ToString(),
                item.Type == FoodType.Ordinary
                    ? BoardObjectKind.OrdinaryFood
                    : BoardObjectKind.SpecialFood,
                ToPosition(item.Position)));
        }

        if (snapshot.Shield.ActiveItem is not null)
        {
            objects.Add(new BoardObjectOutput(
                snapshot.Shield.ActiveItem.ShieldItemId.Value.ToString(),
                BoardObjectKind.Shield,
                ToPosition(snapshot.Shield.ActiveItem.Position)));
        }

        if (snapshot.Fire.ActiveItem is not null)
        {
            objects.Add(new BoardObjectOutput(
                snapshot.Fire.ActiveItem.FireItemId.Value.ToString(),
                BoardObjectKind.Fire,
                ToPosition(snapshot.Fire.ActiveItem.Position)));
        }

        if (snapshot.Bomb.ActiveItem is not null)
        {
            objects.Add(new BoardObjectOutput(
                snapshot.Bomb.ActiveItem.BombItemId.Value.ToString(),
                BoardObjectKind.Bomb,
                ToPosition(snapshot.Bomb.ActiveItem.Position)));
        }

        return objects;
    }

    private List<BoardCellOutput> CreateBoardCells(
        SnakeGameSnapshot snapshot,
        List<BoardObjectOutput> objects)
    {
        Dictionary<GridPosition, BoardCellKind> occupiedCells = new Dictionary<GridPosition, BoardCellKind>();
        foreach (GridPosition wallCell in snapshot.DoorAndMaze.WallCells)
        {
            AddOccupiedCell(occupiedCells, wallCell, BoardCellKind.Wall);
        }

        foreach (Door door in snapshot.DoorAndMaze.Doors)
        {
            AddOccupiedCell(occupiedCells, door.Position, BoardCellKind.Door);
        }

        List<GridPosition> snakeCells = snapshot.Snake.BodyCells;
        for (int index = 0; index < snakeCells.Count; index++)
        {
            GridPosition snakeCell = snakeCells[index];
            BoardCellKind snakeKind = index == 0
                ? BoardCellKind.SnakeHead
                : BoardCellKind.SnakeBody;
            if (occupiedCells.TryGetValue(snakeCell, out BoardCellKind occupiedKind)
                && occupiedKind == BoardCellKind.Door)
            {
                occupiedCells[snakeCell] = snakeKind;
            }
            else
            {
                AddOccupiedCell(occupiedCells, snakeCell, snakeKind);
            }
        }

        foreach (BoardObjectOutput gameObject in objects)
        {
            GridPosition position = new GridPosition(gameObject.Position.X, gameObject.Position.Y);
            AddOccupiedCell(occupiedCells, position, MapObjectKind(gameObject.Kind));
        }

        List<BoardCellOutput> boardCells = new List<BoardCellOutput>();
        for (int y = 0; y < snapshot.Board.Rows; y++)
        {
            for (int x = 0; x < snapshot.Board.Columns; x++)
            {
                GridPosition position = new GridPosition(x, y);
                BoardCellKind kind = occupiedCells.TryGetValue(position, out BoardCellKind occupiedKind)
                    ? occupiedKind
                    : BoardCellKind.Empty;
                boardCells.Add(new BoardCellOutput(new GridPositionDTO(x, y), kind));
            }
        }

        return boardCells;
    }

    private void AddOccupiedCell(
        Dictionary<GridPosition, BoardCellKind> occupiedCells,
        GridPosition position,
        BoardCellKind kind)
    {
        if (!occupiedCells.TryAdd(position, kind))
        {
            throw new InvalidOperationException($"棋盤格位重疊：({position.X}, {position.Y})。");
        }
    }

    private BoardCellKind MapObjectKind(BoardObjectKind kind)
    {
        return kind switch
        {
            BoardObjectKind.OrdinaryFood => BoardCellKind.OrdinaryFood,
            BoardObjectKind.SpecialFood => BoardCellKind.SpecialFood,
            BoardObjectKind.Shield => BoardCellKind.Shield,
            BoardObjectKind.Fire => BoardCellKind.Fire,
            BoardObjectKind.Bomb => BoardCellKind.Bomb,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "不支援的棋盤物件。")
        };
    }

    private DoorVisualState MapDoorState(DoorPhase phase)
    {
        return phase switch
        {
            DoorPhase.Open => DoorVisualState.Open,
            DoorPhase.ClosingWarning => DoorVisualState.ClosingWarning,
            DoorPhase.CloseDeferred => DoorVisualState.CloseDeferred,
            DoorPhase.Closed => DoorVisualState.Closed,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "不支援的門狀態。")
        };
    }

    private List<string> CreateRuleHints(LevelNumber levelNumber)
    {
        List<string> hints = new List<string>
        {
            "普通食物 +1 格",
            "可以穿越棋盤邊界",
            "撞到自己的身體會死亡"
        };
        if (levelNumber != LevelNumber.First)
        {
            hints = new List<string>
            {
                "普通食物 +1 格",
                "特殊食物 +2 格",
                "護盾增加 1 次保命",
                "火焰 -2 格"
            };
        }

        if (levelNumber == LevelNumber.Third)
        {
            hints.Add("炸彈會觸發死亡事件");
            hints.Add("第三關不能穿牆");
        }

        return hints;
    }

    private ResultCardOutput? CreateResultCard(SnakeGameSnapshot snapshot)
    {
        if (snapshot.Game.Phase == GamePhase.Playing)
        {
            return null;
        }

        LevelResult? result = snapshot.GameResult.LatestResult;
        if (result is null)
        {
            return null;
        }

        return new ResultCardOutput(
            result.Outcome.ToString(),
            (int)result.Level,
            result.FinalLength,
            result.DeathCause?.ToString());
    }

    private List<GameAction> CreateAvailableActions(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Ready => new List<GameAction> { GameAction.Start },
            GamePhase.Playing => new List<GameAction> { GameAction.ChangeDirection },
            GamePhase.GameOver => new List<GameAction> { GameAction.Restart },
            GamePhase.Completed => new List<GameAction> { GameAction.Restart },
            _ => new List<GameAction>()
        };
    }

    private GridPositionDTO ToPosition(GridPosition position)
    {
        return new GridPositionDTO(position.X, position.Y);
    }
}
