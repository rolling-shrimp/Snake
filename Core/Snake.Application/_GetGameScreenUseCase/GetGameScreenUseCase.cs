using Snake.Application.Repositories;
using Snake.Application._GetGameScreenUseCase.Enums;
using Snake.Application._GetGameScreenUseCase.Dto;
using Snake.Domain;

namespace Snake.Application._GetGameScreenUseCase
{
    public class GetGameScreenUseCase
    {
        private Game _game;

        public GetGameScreenUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public GameScreenDTO GetGameScreen()
        {
            try
            {
                List<GridPositionDTO> snakeCells = _game.Snake.BodyCells.Select(ToPosition).ToList();
                List<BoardObjectDTO> objects = CreateObjects();
                List<GridPositionDTO> wallCells = _game.DoorAndMaze.WallCells.Select(ToPosition).ToList();
                List<DoorDTO> doors = _game.DoorAndMaze.Doors
                    .Select(door => new DoorDTO(
                        ToPosition(door.Position),
                        MapDoorState(door.Phase)))
                    .ToList();
                LevelStatusDTO levelStatus = new LevelStatusDTO(
                    (int)_game.Level.Number,
                    _game.Snake.Length,
                    _game.Level.TargetLength,
                    _game.Level.GetProgressPercent(_game.Snake.Length),
                    _game.Shield.RemainingCharges);
                return new GameScreenDTO(
                    GetScreenKind(),
                    CreateBoardCells(objects),
                    snakeCells,
                    objects,
                    wallCells,
                    doors,
                    levelStatus,
                    CreateRuleHints(),
                    CreateResult(),
                    CreateAvailableActions());
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("GetGameScreen 執行失敗。", exception);
            }
        }

        private GameScreenKind GetScreenKind()
        {
            if (_game.Phase == GamePhase.GameOver)
            {
                return GameScreenKind.GameOver;
            }

            if (_game.Phase == GamePhase.Completed)
            {
                return GameScreenKind.GameCompleted;
            }

            if (_game.Phase == GamePhase.Ready)
            {
                return _game.Level.Number switch
                {
                    LevelNumber.First => GameScreenKind.FirstLevelReady,
                    LevelNumber.Second => GameScreenKind.SecondLevelReady,
                    LevelNumber.Third => GameScreenKind.ThirdLevelReady,
                    _ => throw new InvalidOperationException("目前關卡無法對應待開始畫面。")
                };
            }

            return _game.Level.Number switch
            {
                LevelNumber.First => GameScreenKind.FirstLevelPlaying,
                LevelNumber.Second => GameScreenKind.SecondLevelPlaying,
                LevelNumber.Third => GameScreenKind.ThirdLevelPlaying,
                _ => throw new InvalidOperationException("目前關卡無法對應遊玩畫面。")
            };
        }

        private List<BoardObjectDTO> CreateObjects()
        {
            List<BoardObjectDTO> objects = new List<BoardObjectDTO>();
            foreach (FoodItem food in _game.Food.ActiveItems.OrderBy(item => item.Type))
            {
                objects.Add(new BoardObjectDTO(
                    food.Type == FoodType.Ordinary ? BoardObjectKind.OrdinaryFood : BoardObjectKind.SpecialFood,
                    ToPosition(food.Position)));
            }

            if (_game.Shield.ActiveItem is not null)
            {
                objects.Add(new BoardObjectDTO(
                    BoardObjectKind.Shield,
                    ToPosition(_game.Shield.ActiveItem.Position)));
            }

            if (_game.Fire.ActiveItem is not null)
            {
                objects.Add(new BoardObjectDTO(
                    BoardObjectKind.Fire,
                    ToPosition(_game.Fire.ActiveItem.Position)));
            }

            if (_game.Bomb.ActiveItem is not null)
            {
                objects.Add(new BoardObjectDTO(
                    BoardObjectKind.Bomb,
                    ToPosition(_game.Bomb.ActiveItem.Position)));
            }

            return objects;
        }

        private List<BoardCellDTO> CreateBoardCells(List<BoardObjectDTO> objects)
        {
            Dictionary<GridPosition, BoardCellKind> occupiedCells = new Dictionary<GridPosition, BoardCellKind>();
            foreach (GridPosition wallCell in _game.DoorAndMaze.WallCells)
            {
                AddOccupiedCell(occupiedCells, wallCell, BoardCellKind.Wall);
            }

            foreach (Door door in _game.DoorAndMaze.Doors)
            {
                AddOccupiedCell(occupiedCells, door.Position, BoardCellKind.Door);
            }

            List<GridPosition> snakeCells = _game.Snake.BodyCells;
            for (int index = 0; index < snakeCells.Count; index++)
            {
                GridPosition snakeCell = snakeCells[index];
                BoardCellKind snakeKind = index == 0 ? BoardCellKind.SnakeHead : BoardCellKind.SnakeBody;
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

            foreach (BoardObjectDTO gameObject in objects)
            {
                AddOccupiedCell(
                    occupiedCells,
                    new GridPosition(gameObject.Position.X, gameObject.Position.Y),
                    MapObjectKind(gameObject.Kind));
            }

            List<BoardCellDTO> boardCells = new List<BoardCellDTO>();
            for (int y = 0; y < _game.Board.Rows; y++)
            {
                for (int x = 0; x < _game.Board.Columns; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    BoardCellKind kind = occupiedCells.TryGetValue(position, out BoardCellKind occupiedKind)
                        ? occupiedKind
                        : BoardCellKind.Empty;
                    boardCells.Add(new BoardCellDTO(new GridPositionDTO(x, y), kind));
                }
            }

            return boardCells;
        }

        private void AddOccupiedCell(Dictionary<GridPosition, BoardCellKind> occupiedCells, GridPosition position, BoardCellKind kind)
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

        private List<string> CreateRuleHints()
        {
            if (_game.Level.Number == LevelNumber.First)
            {
                return new List<string> { "普通食物 +1 格", "可以穿越棋盤邊界", "撞到自己的身體會死亡" };
            }

            List<string> hints = new List<string>
            {
                "普通食物 +1 格",
                "特殊食物 +2 格",
                "護盾增加 1 次保命",
                "火焰 -2 格"
            };
            if (_game.Level.Number == LevelNumber.Third)
            {
                hints.Add("炸彈會觸發死亡事件");
                hints.Add("第三關不能穿牆");
            }

            return hints;
        }

        private ResultCardDTO? CreateResult()
        {
            if (_game.Phase == GamePhase.Playing)
            {
                return null;
            }

            LevelResult? result = _game.GameResult.LatestResult;
            if (result is null)
            {
                return null;
            }

            return new ResultCardDTO(result.Outcome.ToString(), (int)result.Level, result.FinalLength, result.DeathCause?.ToString());
        }

        private List<GameAction> CreateAvailableActions()
        {
            return _game.Phase switch
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
}


