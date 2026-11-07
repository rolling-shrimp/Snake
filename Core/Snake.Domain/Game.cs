namespace Snake.Domain
{
    public class Game
    {
        private GamePhase _phase;

        public Game()
        {
            Board = new Board();
            Snake = new Snake(Board.GetFirstLevelSafeStart());
            Level = new Level();
            Food = new Food();
            Fire = new Fire();
            Shield = new Shield();
            Bomb = new Bomb();
            DoorAndMaze = new DoorAndMaze();
            GameResult = new GameResult();
            _phase = GamePhase.Ready;
        }

        public GamePhase Phase => _phase;
        public bool CanStart => _phase == GamePhase.Ready;
        public bool MovementActive => _phase == GamePhase.Playing;
        internal Board Board { get; }
        internal Snake Snake { get; }
        internal Level Level { get; }
        internal Food Food { get; }
        internal Fire Fire { get; }
        internal Shield Shield { get; }
        internal Bomb Bomb { get; }
        internal DoorAndMaze DoorAndMaze { get; }
        internal GameResult GameResult { get; }
        internal GridPosition? NextHead { get; private set; }
        internal SnakeMovementPlan? MovementPlan { get; private set; }
        internal DeathCause? PendingDeathCause { get; private set; }
        internal int PreHitLength { get; private set; }
        internal bool HasMovedThisTick { get; private set; }
        internal bool LevelCompletionPending { get; set; }
        internal bool NextLevelPreparationPending { get; set; }
        internal int DoorTickCount { get; set; }

        public bool StartLevel()
        {
            if (_phase != GamePhase.Ready)
            {
                return false;
            }

            _phase = GamePhase.Playing;
            return true;
        }

        public void PrepareNextLevel()
        {
            _phase = GamePhase.Ready;
        }

        public void Complete()
        {
            if (_phase == GamePhase.Playing)
            {
                _phase = GamePhase.Completed;
            }
        }

        public void EndGame()
        {
            if (_phase == GamePhase.Playing)
            {
                _phase = GamePhase.GameOver;
            }
        }

        public void RestartPlaying()
        {
            if (_phase == GamePhase.GameOver || _phase == GamePhase.Completed)
            {
                _phase = GamePhase.Playing;
            }
        }

        internal void BeginTick()
        {
            NextHead = null;
            MovementPlan = null;
            PendingDeathCause = null;
            PreHitLength = Snake.Length;
            HasMovedThisTick = false;
            LevelCompletionPending = false;
        }

        internal void PrepareMovement()
        {
            if (_phase != GamePhase.Playing
                || MovementPlan is not null
                || NextHead is not null
                || PendingDeathCause is not null)
            {
                return;
            }

            GridPosition currentHead = Snake.BodyCells[0];
            BoardMovementResult movement = Board.ResolveMovement(
                currentHead,
                Snake.NextDirection,
                Level.BoundaryPolicy);
            if (movement.Position is null)
            {
                MarkDeath(DeathCause.BoundaryCollision, Snake.Length);
                return;
            }

            NextHead = movement.Position;
            FoodItem? contactedFood = Food.ActiveItems.FirstOrDefault(item => item.Position.Equals(NextHead));
            int growthDelta = contactedFood?.Type switch
            {
                FoodType.Ordinary => Food.OrdinaryLengthDelta,
                FoodType.Special => Food.SpecialLengthDelta,
                _ => 0
            };
            SnakeMovementPlanResult planResult = Snake.PlanMovement(
                NextHead,
                growthDelta,
                Board,
                Level.BoundaryPolicy,
                GetMovementBlockedCells());
            if (!planResult.IsSuccess || planResult.Plan is null)
            {
                throw new InvalidOperationException(
                    planResult.FailureMessage ?? "無法建立合法蛇身移動計畫。");
            }

            MovementPlan = planResult.Plan;
        }

        internal void MarkMovementCommitted()
        {
            if (MovementPlan is null || PendingDeathCause is not null)
            {
                return;
            }

            HasMovedThisTick = true;
        }

        internal void MarkDeath(DeathCause deathCause, int preHitLength)
        {
            if (PendingDeathCause is not null)
            {
                return;
            }

            PendingDeathCause = deathCause;
            PreHitLength = preHitLength;
        }

        internal void ClearPendingDeath()
        {
            PendingDeathCause = null;
        }

        internal List<GridPosition> GetObjectOccupiedCells()
        {
            List<GridPosition> occupiedCells = Food.GetOccupiedCells();
            occupiedCells.AddRange(Fire.GetOccupiedCells());
            occupiedCells.AddRange(Shield.GetOccupiedCells());
            occupiedCells.AddRange(Bomb.GetOccupiedCells());
            return occupiedCells;
        }

        internal List<GridPosition> GetMovementBlockedCells()
        {
            List<GridPosition> blockedCells = GetObjectOccupiedCells();
            blockedCells.AddRange(DoorAndMaze.WallCells);
            blockedCells.AddRange(DoorAndMaze.Doors
                .Where(door => door.Phase == DoorPhase.Closed)
                .Select(door => door.Position));
            return blockedCells;
        }

        internal List<GridPosition> GetSpawnBlockedCells()
        {
            List<GridPosition> blockedCells = GetObjectOccupiedCells();
            blockedCells.AddRange(DoorAndMaze.WallCells);
            blockedCells.AddRange(DoorAndMaze.Doors.Select(door => door.Position));
            return blockedCells;
        }

        internal void ResetObjects()
        {
            Food.Reset();
            Shield.Reset();
            Fire.Reset();
            Bomb.Reset();
        }

        internal void ResetTransientState()
        {
            BeginTick();
            NextLevelPreparationPending = false;
            DoorTickCount = 0;
        }
    }
}
