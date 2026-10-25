using Snake.Entities;

namespace Snake.UseCases;

public class SnakeGameSession :
    IFirstLevelReadPort,
    ISecondLevelReadyBoardReadPort,
    ISecondLevelRulesReadPort,
    ISecondLevelStatusReadPort,
    IThirdLevelReadyBoardReadPort,
    IThirdLevelRulesReadPort,
    IThirdLevelStatusReadPort,
    IThirdLevelDoorStatusReadPort,
    ILevelPassedResultReadPort,
    IGameOverResultReadPort,
    IGameCompletedResultReadPort,
    IGameplayReadPort,
    ILevelStatusReadPort
{
    private Game _game;
    private Board _board;
    private Snake.Entities.Snake _snake;
    private Level _level;
    private Food _food;
    private Fire _fire;
    private Shield _shield;
    private Bomb _bomb;
    private DoorAndMaze _doorAndMaze;
    private GameResult _gameResult;
    private IDomainEvent? _lastMovementFact;
    private List<IDomainEvent> _publishedFacts;

    public SnakeGameSession()
    {
        _board = new Board(new BoardId(Guid.NewGuid()));
        _snake = new Snake.Entities.Snake(new SnakeId(Guid.NewGuid()), _board.GetFirstLevelSafeStart());
        _level = new Level(new LevelId(Guid.NewGuid()));
        _food = new Food(new FoodId(Guid.NewGuid()));
        _fire = new Fire(new FireId(Guid.NewGuid()));
        _shield = new Shield(new ShieldId(Guid.NewGuid()));
        _bomb = new Bomb(new BombId(Guid.NewGuid()));
        _doorAndMaze = new DoorAndMaze(new DoorAndMazeId(Guid.NewGuid()));
        _gameResult = new GameResult(new GameResultId(Guid.NewGuid()));
        _game = new Game(new GameId(Guid.NewGuid()));
        _publishedFacts = new List<IDomainEvent>();
    }

    internal SnakeGameSession(
        Game game,
        Board board,
        Snake.Entities.Snake snake,
        Level level,
        Food food)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _board = board ?? throw new ArgumentNullException(nameof(board));
        _snake = snake ?? throw new ArgumentNullException(nameof(snake));
        _level = level ?? throw new ArgumentNullException(nameof(level));
        _food = food ?? throw new ArgumentNullException(nameof(food));
        _fire = new Fire(new FireId(Guid.NewGuid()));
        _shield = new Shield(new ShieldId(Guid.NewGuid()));
        _bomb = new Bomb(new BombId(Guid.NewGuid()));
        _doorAndMaze = new DoorAndMaze(new DoorAndMazeId(Guid.NewGuid()));
        _gameResult = new GameResult(new GameResultId(Guid.NewGuid()));
        _publishedFacts = new List<IDomainEvent>();
    }

    internal Game Game => _game;
    internal Board Board => _board;
    internal Snake.Entities.Snake Snake => _snake;
    internal Level Level => _level;
    internal Food Food => _food;
    internal Fire Fire => _fire;
    internal Shield Shield => _shield;
    internal Bomb Bomb => _bomb;
    internal DoorAndMaze DoorAndMaze => _doorAndMaze;
    internal GameResult GameResult => _gameResult;
    internal IDomainEvent? LastMovementFact => _lastMovementFact;
    internal List<IDomainEvent> PublishedFacts => new List<IDomainEvent>(_publishedFacts);

    internal void RecordMovementFact(IDomainEvent? movementFact)
    {
        _lastMovementFact = movementFact;
    }

    internal void PublishFact(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _publishedFacts.Add(domainEvent);
    }

    internal List<GridPosition> GetObjectOccupiedCells()
    {
        List<GridPosition> occupiedCells = _food.GetOccupiedCells();
        occupiedCells.AddRange(_fire.GetOccupiedCells());
        occupiedCells.AddRange(_shield.GetOccupiedCells());
        occupiedCells.AddRange(_bomb.GetOccupiedCells());
        occupiedCells.AddRange(_doorAndMaze.GetUnavailableCells());
        return occupiedCells;
    }

    public FirstLevelReadModel Read()
    {
        List<GridPositionDTO> snakeCells = _snake.BodyCells
            .Select(cell => new GridPositionDTO(cell.X, cell.Y))
            .ToList();
        return new FirstLevelReadModel(
            _game.Phase == GamePhase.Ready,
            (int)_level.Number,
            _board.Rows,
            _board.Columns,
            snakeCells,
            _game.CanStart,
            _food.OrdinaryLengthDelta,
            _level.BoundaryPolicy == BoundaryPolicy.Wrap,
            _snake.IsSelfCollisionFatal,
            _level.TargetLength,
            _level.GetProgressPercent(_snake.Length));
    }

    bool ISecondLevelReadyBoardReadPort.IsAvailable =>
        _game.Phase == GamePhase.Ready && _level.Number == LevelNumber.Second;

    SecondLevelReadyBoardRspDTO ISecondLevelReadyBoardReadPort.Read()
    {
        List<GridPositionDTO> snakeCells = _snake.BodyCells
            .Select(cell => new GridPositionDTO(cell.X, cell.Y))
            .ToList();
        return new SecondLevelReadyBoardRspDTO(
            _board.Rows,
            _board.Columns,
            snakeCells,
            _game.CanStart);
    }

    bool ISecondLevelRulesReadPort.IsAvailable =>
        _game.Phase == GamePhase.Ready && _level.Number == LevelNumber.Second;

    SecondLevelRulesRspDTO ISecondLevelRulesReadPort.Read()
    {
        List<RuleHintDTO> ruleHints = new List<RuleHintDTO>
        {
            new RuleHintDTO("ORDINARY_FOOD_GROWTH", $"普通食物 +{_food.OrdinaryLengthDelta} 格"),
            new RuleHintDTO("SPECIAL_FOOD_GROWTH", $"特殊食物 +{_food.SpecialLengthDelta} 格"),
            new RuleHintDTO("SHIELD_RESCUE", "護盾 +1 次保命"),
            new RuleHintDTO("FIRE_SHRINK", $"火焰 −{Math.Abs(_fire.LengthDelta)} 格")
        };
        return new SecondLevelRulesRspDTO(ruleHints);
    }

    bool ISecondLevelStatusReadPort.IsAvailable =>
        _game.Phase == GamePhase.Ready && _level.Number == LevelNumber.Second;

    SecondLevelStatusRspDTO ISecondLevelStatusReadPort.Read()
    {
        return new SecondLevelStatusRspDTO(
            "第二關",
            _snake.Length,
            _level.TargetLength,
            _level.GetProgressPercent(_snake.Length),
            _shield.RemainingCharges);
    }

    bool IThirdLevelReadyBoardReadPort.IsAvailable =>
        _game.Phase == GamePhase.Ready && _level.Number == LevelNumber.Third;

    ThirdLevelReadyBoardRspDTO IThirdLevelReadyBoardReadPort.Read()
    {
        List<GridPositionDTO> snakeCells = _snake.BodyCells
            .Select(ToGridPositionDTO)
            .ToList();
        List<GridPositionDTO> wallCells = _doorAndMaze.WallCells
            .Select(ToGridPositionDTO)
            .ToList();
        List<DoorStatusDTO> doors = _doorAndMaze.Doors
            .Select(ToDoorStatusDTO)
            .ToList();
        return new ThirdLevelReadyBoardRspDTO(
            _board.Rows,
            _board.Columns,
            snakeCells,
            wallCells,
            doors,
            _game.CanStart);
    }

    bool IThirdLevelRulesReadPort.IsAvailable =>
        _game.Phase == GamePhase.Ready && _level.Number == LevelNumber.Third;

    ThirdLevelRulesRspDTO IThirdLevelRulesReadPort.Read()
    {
        List<RuleHintDTO> ruleHints = new List<RuleHintDTO>
        {
            new RuleHintDTO("ORDINARY_FOOD_GROWTH", $"普通食物 +{_food.OrdinaryLengthDelta} 格"),
            new RuleHintDTO("SPECIAL_FOOD_GROWTH", $"特殊食物 +{_food.SpecialLengthDelta} 格"),
            new RuleHintDTO("SHIELD_RESCUE", "護盾 +1"),
            new RuleHintDTO("FIRE_SHRINK", $"火焰 −{Math.Abs(_fire.LengthDelta)} 格"),
            new RuleHintDTO("BOMB_FATAL", "炸彈致命"),
            new RuleHintDTO("WALL_BLOCKED", "不能穿牆")
        };
        return new ThirdLevelRulesRspDTO(ruleHints);
    }

    bool IThirdLevelStatusReadPort.IsAvailable =>
        _game.Phase == GamePhase.Ready && _level.Number == LevelNumber.Third;

    ThirdLevelStatusRspDTO IThirdLevelStatusReadPort.Read()
    {
        return new ThirdLevelStatusRspDTO(
            "第三關",
            _snake.Length,
            _level.TargetLength,
            _level.GetProgressPercent(_snake.Length),
            _shield.RemainingCharges);
    }

    bool IThirdLevelDoorStatusReadPort.IsAvailable =>
        _level.Number == LevelNumber.Third
        && (_game.Phase == GamePhase.Ready || _game.Phase == GamePhase.Playing);

    ThirdLevelDoorStatusRspDTO IThirdLevelDoorStatusReadPort.Read()
    {
        List<DoorStatusDTO> doors = _doorAndMaze.Doors
            .Select(ToDoorStatusDTO)
            .ToList();
        return new ThirdLevelDoorStatusRspDTO(
            doors,
            _game.Phase == GamePhase.Playing);
    }

    bool ILevelPassedResultReadPort.IsAvailable
    {
        get
        {
            LevelResult? latestResult = _gameResult.LatestResult;
            return _game.Phase == GamePhase.Ready
                && latestResult is not null
                && latestResult.Outcome == global::Snake.Entities.LevelOutcome.Passed
                && latestResult.Level != LevelNumber.Third
                && (int)_level.Number == (int)latestResult.Level + 1;
        }
    }

    LevelPassedResultRspDTO ILevelPassedResultReadPort.Read()
    {
        LevelResult latestResult = GetLatestResult();
        return new LevelPassedResultRspDTO(
            (int)latestResult.Level,
            latestResult.FinalLength,
            "關卡通過",
            true);
    }

    bool IGameOverResultReadPort.IsAvailable
    {
        get
        {
            LevelResult? latestResult = _gameResult.LatestResult;
            return _game.Phase == GamePhase.GameOver
                && latestResult is not null
                && latestResult.Outcome == global::Snake.Entities.LevelOutcome.Failed
                && latestResult.DeathCause is not null;
        }
    }

    GameOverResultRspDTO IGameOverResultReadPort.Read()
    {
        LevelResult latestResult = GetLatestResult();
        global::Snake.Entities.DeathCause deathCause = latestResult.DeathCause
            ?? throw new InvalidOperationException("失敗結果缺少死亡原因。");
        int targetLength = latestResult.Level == LevelNumber.Third ? 50 : 20;
        int progressPercent = Math.Clamp(latestResult.FinalLength * 100 / targetLength, 0, 100);
        return new GameOverResultRspDTO(
            (int)latestResult.Level,
            latestResult.FinalLength,
            MapDeathCause(deathCause),
            progressPercent,
            true);
    }

    bool IGameCompletedResultReadPort.IsAvailable
    {
        get
        {
            LevelResult? latestResult = _gameResult.LatestResult;
            return _game.Phase == GamePhase.Completed
                && latestResult is not null
                && latestResult.Level == LevelNumber.Third
                && latestResult.Outcome == global::Snake.Entities.LevelOutcome.Passed;
        }
    }

    GameCompletedResultRspDTO IGameCompletedResultReadPort.Read()
    {
        LevelResult latestResult = GetLatestResult();
        return new GameCompletedResultRspDTO(
            latestResult.FinalLength,
            100,
            "闖關成功",
            true);
    }

    public GameplayReadModel ReadGameplay()
    {
        List<GameObjectStateDTO> objects = new List<GameObjectStateDTO>();
        objects.AddRange(_food.ActiveItems.Select(item => new GameObjectStateDTO(
            item.Type == FoodType.Ordinary ? GameObjectType.Strawberry : GameObjectType.SpecialFood,
            item.FoodItemId.Value.ToString(),
            ToGridPositionDTO(item.Position))));
        if (_shield.ActiveItem is not null)
        {
            objects.Add(new GameObjectStateDTO(
                GameObjectType.Shield,
                _shield.ActiveItem.ShieldItemId.Value.ToString(),
                ToGridPositionDTO(_shield.ActiveItem.Position)));
        }

        if (_fire.ActiveItem is not null)
        {
            objects.Add(new GameObjectStateDTO(
                GameObjectType.Fire,
                _fire.ActiveItem.FireItemId.Value.ToString(),
                ToGridPositionDTO(_fire.ActiveItem.Position)));
        }

        if (_bomb.ActiveItem is not null)
        {
            objects.Add(new GameObjectStateDTO(
                GameObjectType.Bomb,
                _bomb.ActiveItem.BombItemId.Value.ToString(),
                ToGridPositionDTO(_bomb.ActiveItem.Position)));
        }

        return new GameplayReadModel(
            MapGameplayPhase(_game.Phase),
            (int)_level.Number,
            _board.Rows,
            _board.Columns,
            _snake.BodyCells.Select(ToGridPositionDTO).ToList(),
            objects,
            _doorAndMaze.WallCells.Select(ToGridPositionDTO).ToList(),
            _doorAndMaze.Doors.Select(ToDoorStatusDTO).ToList(),
            _snake.Length,
            _level.TargetLength,
            _level.GetProgressPercent(_snake.Length),
            _shield.RemainingCharges,
            _game.MovementActive,
            _snake.MovementIntervalMilliseconds,
            _doorAndMaze.ToggleIntervalMilliseconds,
            _doorAndMaze.ClosingWarningMilliseconds,
            MapMovementCollision());
    }

    LevelStatusSourceDTO ILevelStatusReadPort.Read()
    {
        int? shieldRemainingCharges = _level.Number == LevelNumber.First
            ? null
            : _shield.RemainingCharges;
        return new LevelStatusSourceDTO(
            (int)_level.Number,
            _snake.Length,
            _level.TargetLength,
            shieldRemainingCharges);
    }

    private GridPositionDTO ToGridPositionDTO(GridPosition position)
    {
        return new GridPositionDTO(position.X, position.Y);
    }

    private DoorStatusDTO ToDoorStatusDTO(Door door)
    {
        return new DoorStatusDTO(
            door.DoorId.Value,
            ToGridPositionDTO(door.Position),
            door.Phase.ToString());
    }

    private LevelResult GetLatestResult()
    {
        return _gameResult.LatestResult
            ?? throw new InvalidOperationException("目前沒有關卡結果。");
    }

    private DeathCause MapDeathCause(global::Snake.Entities.DeathCause deathCause)
    {
        return deathCause switch
        {
            global::Snake.Entities.DeathCause.SelfCollision => DeathCause.SelfCollision,
            global::Snake.Entities.DeathCause.WallCollision => DeathCause.WallCollision,
            global::Snake.Entities.DeathCause.ClosedDoorCollision => DeathCause.ClosedDoorCollision,
            global::Snake.Entities.DeathCause.Bomb => DeathCause.Bomb,
            global::Snake.Entities.DeathCause.FatalFire => DeathCause.FatalFire,
            _ => throw new ArgumentOutOfRangeException(nameof(deathCause), deathCause, "不支援的死亡原因。")
        };
    }

    private GameplayPhase MapGameplayPhase(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Ready => GameplayPhase.Ready,
            GamePhase.Playing => GameplayPhase.Playing,
            GamePhase.GameOver => GameplayPhase.GameOver,
            GamePhase.Completed => GameplayPhase.Completed,
            _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "不支援的遊戲階段。")
        };
    }

    private GameplayCollisionDTO? MapMovementCollision()
    {
        if (_lastMovementFact is SelfCollisionDomainEvent selfCollision)
        {
            return new GameplayCollisionDTO(
                GameplayCollisionType.SelfCollision,
                ToGridPositionDTO(selfCollision.CollisionPosition),
                null);
        }

        if (_lastMovementFact is WallCollisionDomainEvent wallCollision)
        {
            return new GameplayCollisionDTO(
                GameplayCollisionType.WallCollision,
                new GridPositionDTO(wallCollision.AttemptedX, wallCollision.AttemptedY),
                null);
        }

        if (_lastMovementFact is ClosedDoorCollisionDomainEvent doorCollision)
        {
            Door door = _doorAndMaze.FindDoor(doorCollision.DoorId)
                ?? throw new InvalidOperationException("碰撞事件對應的門不存在。");
            return new GameplayCollisionDTO(
                GameplayCollisionType.ClosedDoorCollision,
                ToGridPositionDTO(door.Position),
                doorCollision.DoorId.Value.ToString());
        }

        return null;
    }
}
