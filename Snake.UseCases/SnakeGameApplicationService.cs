namespace Snake.UseCases;

public class SnakeGameApplicationService : ISnakeGameApplicationService
{
    private IGameSnapshotStore _snapshotStore;
    private ISpawnPositionSelector _spawnPositionSelector;
    private GameScreenOutputFactory _screenOutputFactory;

    public SnakeGameApplicationService(
        IGameSnapshotStore snapshotStore,
        ISpawnPositionSelector spawnPositionSelector)
    {
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _spawnPositionSelector = spawnPositionSelector
            ?? throw new ArgumentNullException(nameof(spawnPositionSelector));
        _screenOutputFactory = new GameScreenOutputFactory();
    }

    public GetCurrentScreenResult GetCurrentScreen(GetCurrentScreenQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        try
        {
            VersionedSnakeGameSnapshot versionedSnapshot = _snapshotStore.Load();
            GameScreenOutput screen = _screenOutputFactory.Create(versionedSnapshot.Snapshot);
            return new GetCurrentScreenResult(
                true,
                GetCurrentScreenOutcome.ScreenReturned,
                null,
                null,
                screen,
                RuntimeDirective.Unchanged);
        }
        catch (InvalidOperationException exception)
        {
            return new GetCurrentScreenResult(
                false,
                GetCurrentScreenOutcome.Failed,
                "UnsupportedScreenState",
                exception.Message,
                null,
                RuntimeDirective.Unchanged);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("GetCurrentScreen workflow 執行失敗。", exception);
        }
    }

    public StartCurrentLevelResult StartCurrentLevel(StartCurrentLevelCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            VersionedSnakeGameSnapshot versionedSnapshot = _snapshotStore.Load();
            SnakeGameSnapshot snapshot = versionedSnapshot.Snapshot;
            SnakeGameSnapshot originalSnapshot = snapshot.Copy();
            if (snapshot.Game.Phase != Snake.Entities.GamePhase.Ready)
            {
                return new StartCurrentLevelResult(
                    true,
                    StartCurrentLevelOutcome.NotReady,
                    null,
                    null,
                    _screenOutputFactory.Create(snapshot),
                    RuntimeDirective.Unchanged);
            }

            if (!HasValidReadyState(snapshot))
            {
                return CreateStartFailure(
                    "InvalidReadyState",
                    "目前關卡的待開始狀態不完整。",
                    originalSnapshot);
            }

            if (!TrySpawnInitialObjects(snapshot))
            {
                return CreateStartFailure(
                    "InitialSpawnUnavailable",
                    "沒有足夠合法格位產生本關首批物件。",
                    originalSnapshot);
            }

            if (!snapshot.Game.StartLevel())
            {
                return CreateStartFailure(
                    "InvalidReadyState",
                    "目前遊戲狀態無法開始關卡。",
                    originalSnapshot);
            }

            if (!_snapshotStore.TryCommit(versionedSnapshot.Version, snapshot))
            {
                return CreateStartFailure(
                    "AtomicCommitFailed",
                    "關卡開始狀態提交失敗。",
                    _snapshotStore.Load().Snapshot);
            }

            return new StartCurrentLevelResult(
                true,
                StartCurrentLevelOutcome.Started,
                null,
                null,
                _screenOutputFactory.Create(snapshot),
                RuntimeDirective.StartOrReset);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("StartCurrentLevel workflow 執行失敗。", exception);
        }
    }

    public SetSnakeDirectionResult SetSnakeDirection(SetSnakeDirectionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            VersionedSnakeGameSnapshot versionedSnapshot = _snapshotStore.Load();
            SnakeGameSnapshot snapshot = versionedSnapshot.Snapshot;
            if (snapshot.Game.Phase != Snake.Entities.GamePhase.Playing)
            {
                return new SetSnakeDirectionResult(
                    true,
                    SetSnakeDirectionOutcome.NotPlaying,
                    null,
                    null,
                    _screenOutputFactory.Create(snapshot),
                    RuntimeDirective.Unchanged);
            }

            Snake.Entities.Direction domainDirection = MapDirection(command.Direction);
            if (!snapshot.Snake.QueueDirection(domainDirection))
            {
                return new SetSnakeDirectionResult(
                    true,
                    SetSnakeDirectionOutcome.OppositeDirection,
                    null,
                    null,
                    _screenOutputFactory.Create(snapshot),
                    RuntimeDirective.Unchanged);
            }

            if (!_snapshotStore.TryCommit(versionedSnapshot.Version, snapshot))
            {
                return new SetSnakeDirectionResult(
                    false,
                    SetSnakeDirectionOutcome.Failed,
                    "DirectionCommitConflict",
                    "方向更新時遊戲狀態已改變。",
                    _screenOutputFactory.Create(_snapshotStore.Load().Snapshot),
                    RuntimeDirective.Unchanged);
            }

            return new SetSnakeDirectionResult(
                true,
                SetSnakeDirectionOutcome.Accepted,
                null,
                null,
                _screenOutputFactory.Create(snapshot),
                RuntimeDirective.Unchanged);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return new SetSnakeDirectionResult(
                false,
                SetSnakeDirectionOutcome.Failed,
                "InvalidDirection",
                exception.Message,
                null,
                RuntimeDirective.Unchanged);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("SetSnakeDirection workflow 執行失敗。", exception);
        }
    }

    public RestartGameResult RestartGame(RestartGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            VersionedSnakeGameSnapshot versionedSnapshot = _snapshotStore.Load();
            SnakeGameSnapshot snapshot = versionedSnapshot.Snapshot;
            if (snapshot.Game.Phase != Snake.Entities.GamePhase.GameOver
                && snapshot.Game.Phase != Snake.Entities.GamePhase.Completed)
            {
                return new RestartGameResult(
                    true,
                    RestartGameOutcome.NotTerminal,
                    null,
                    null,
                    _screenOutputFactory.Create(snapshot),
                    RuntimeDirective.Unchanged);
            }

            snapshot.Level.Reset();
            snapshot.Food.Reset();
            snapshot.Shield.Reset();
            snapshot.Fire.Reset();
            snapshot.Bomb.Reset();
            snapshot.DoorAndMaze.Reset();
            snapshot.GameResult.Reset();
            List<Snake.Entities.GridPosition> safeBody = snapshot.Board.GetFirstLevelSafeStart();
            snapshot.Snake.ResetSafely(
                safeBody,
                snapshot.Board,
                Snake.Entities.BoundaryPolicy.Wrap);
            Snake.Entities.GridPosition? foodPosition = SelectSpawnPosition(snapshot);
            if (foodPosition is null)
            {
                return CreateRestartFailure(
                    "InitialFoodUnavailable",
                    "沒有合法格位產生第一關普通食物。",
                    _snapshotStore.Load().Snapshot);
            }

            snapshot.Food.SpawnOrdinary(
                new Snake.Entities.FoodItemId(Guid.NewGuid()),
                foodPosition);
            snapshot.Game.RestartPlaying();
            if (!_snapshotStore.TryCommit(versionedSnapshot.Version, snapshot))
            {
                return CreateRestartFailure(
                    "AtomicCommitFailed",
                    "新遊戲狀態提交失敗。",
                    _snapshotStore.Load().Snapshot);
            }

            return new RestartGameResult(
                true,
                RestartGameOutcome.Restarted,
                null,
                null,
                _screenOutputFactory.Create(snapshot),
                RuntimeDirective.StartOrReset);
        }
        catch (InvalidOperationException exception)
        {
            return CreateRestartFailure(
                "SafeBodyUnavailable",
                exception.Message,
                _snapshotStore.Load().Snapshot);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("RestartGame workflow 執行失敗。", exception);
        }
    }

    public ProcessGameTickResult ProcessGameTick(ProcessGameTickCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            VersionedSnakeGameSnapshot versionedSnapshot = _snapshotStore.Load();
            SnakeGameSnapshot snapshot = versionedSnapshot.Snapshot;
            SnakeGameSnapshot originalSnapshot = snapshot.Copy();
            if (command.AccumulatedElapsedMilliseconds < 0
                || command.AccumulatedElapsedMilliseconds < snapshot.Game.LastProcessedElapsedMilliseconds)
            {
                return CreateTickFailure(
                    "InvalidElapsedTime",
                    "累積時間不得為負數或小於已處理時間。",
                    originalSnapshot);
            }

            if (snapshot.Game.Phase != Snake.Entities.GamePhase.Playing)
            {
                return new ProcessGameTickResult(
                    true,
                    ProcessGameTickOutcome.NotPlaying,
                    null,
                    null,
                    _screenOutputFactory.Create(snapshot),
                    RuntimeDirective.Unchanged,
                    new List<GameTickEventDTO>());
            }

            List<ScheduledGameEvent> dueEvents = CollectDueEvents(
                snapshot,
                command.AccumulatedElapsedMilliseconds);
            if (dueEvents.Count == 0)
            {
                snapshot.Game.CommitTickTime(command.AccumulatedElapsedMilliseconds);
                if (!_snapshotStore.TryCommit(versionedSnapshot.Version, snapshot))
                {
                    return CreateTickFailure(
                        "AtomicCommitFailed",
                        "Tick 時間基準提交失敗。",
                        originalSnapshot);
                }

                return new ProcessGameTickResult(
                    true,
                    ProcessGameTickOutcome.NoDueEvents,
                    null,
                    null,
                    _screenOutputFactory.Create(snapshot),
                    RuntimeDirective.Unchanged,
                    new List<GameTickEventDTO>());
            }

            List<GameTickEventDTO> processedEvents = new List<GameTickEventDTO>();
            bool shieldRescued = false;
            foreach (ScheduledGameEvent dueEvent in dueEvents)
            {
                if (snapshot.Game.Phase != Snake.Entities.GamePhase.Playing)
                {
                    break;
                }

                if (dueEvent.EventType == GameTickEventType.DoorClosingWarning)
                {
                    snapshot.DoorAndMaze.ApplyClosingWarning();
                    processedEvents.Add(new GameTickEventDTO(
                        dueEvent.DueElapsedMilliseconds,
                        processedEvents.Count + 1,
                        GameTickEventType.DoorClosingWarning,
                        "ClosingWarning"));
                    continue;
                }

                if (dueEvent.EventType == GameTickEventType.DoorToggle)
                {
                    bool deferred = snapshot.DoorAndMaze.ApplyToggle(snapshot.Snake.BodyCells);
                    processedEvents.Add(new GameTickEventDTO(
                        dueEvent.DueElapsedMilliseconds,
                        processedEvents.Count + 1,
                        deferred ? GameTickEventType.DoorCloseDeferred : GameTickEventType.DoorToggle,
                        deferred ? "CloseDeferred" : "Toggled"));
                    continue;
                }

                Snake.Entities.GridPosition currentHead = snapshot.Snake.BodyCells[0];
                Snake.Entities.BoardMovementResult movement = snapshot.Board.ResolveMovement(
                    currentHead,
                    snapshot.Snake.NextDirection,
                    snapshot.Level.BoundaryPolicy);
                if (movement.Position is null)
                {
                    DeathResolutionOutcome boundaryOutcome = ResolveDeath(
                        snapshot,
                        Snake.Entities.DeathCause.BoundaryCollision,
                        snapshot.Snake.Length);
                    if (boundaryOutcome == DeathResolutionOutcome.SnakeTopologyUnavailable)
                    {
                        return CreateTickFailure(
                            "SnakeTopologyUnavailable",
                            "護盾保命時找不到合法安全蛇身。",
                            originalSnapshot);
                    }

                    shieldRescued = boundaryOutcome == DeathResolutionOutcome.ShieldRescued;
                    AddDeathEvent(processedEvents, dueEvent.DueElapsedMilliseconds, boundaryOutcome);
                    continue;
                }

                Snake.Entities.GridPosition nextHead = movement.Position;
                Snake.Entities.FoodItem? contactedFood = snapshot.Food.ActiveItems
                    .FirstOrDefault(item => item.Position.Equals(nextHead));
                int growthDelta = contactedFood?.Type switch
                {
                    Snake.Entities.FoodType.Ordinary => 1,
                    Snake.Entities.FoodType.Special => 2,
                    _ => 0
                };
                List<Snake.Entities.GridPosition> tailBlockedCells = GetTailBlockedCells(snapshot);
                Snake.Entities.SnakeMovementPlanResult planResult = snapshot.Snake.PlanMovement(
                    nextHead,
                    growthDelta,
                    snapshot.Board,
                    snapshot.Level.BoundaryPolicy,
                    tailBlockedCells);
                if (!planResult.IsSuccess || planResult.Plan is null)
                {
                    return CreateTickFailure(
                        planResult.FailureCode ?? "SnakeTopologyUnavailable",
                        planResult.FailureMessage ?? "無法建立合法蛇身移動計畫。",
                        originalSnapshot);
                }

                Snake.Entities.DeathCause? collisionCause = GetCollisionCause(
                    snapshot,
                    nextHead,
                    planResult.Plan);
                if (collisionCause is not null)
                {
                    DeathResolutionOutcome collisionOutcome = ResolveDeath(
                        snapshot,
                        collisionCause.Value,
                        snapshot.Snake.Length);
                    if (collisionOutcome == DeathResolutionOutcome.SnakeTopologyUnavailable)
                    {
                        return CreateTickFailure(
                            "SnakeTopologyUnavailable",
                            "護盾保命時找不到合法安全蛇身。",
                            originalSnapshot);
                    }

                    shieldRescued = shieldRescued
                        || collisionOutcome == DeathResolutionOutcome.ShieldRescued;
                    AddDeathEvent(processedEvents, dueEvent.DueElapsedMilliseconds, collisionOutcome);
                    continue;
                }

                snapshot.Snake.CommitMovement(
                    planResult.Plan,
                    snapshot.Board,
                    snapshot.Level.BoundaryPolicy);
                processedEvents.Add(new GameTickEventDTO(
                    dueEvent.DueElapsedMilliseconds,
                    processedEvents.Count + 1,
                    GameTickEventType.SnakeMoved,
                    "Moved"));
                DeathResolutionOutcome? contactDeath = ApplyObjectContact(
                    snapshot,
                    nextHead,
                    contactedFood,
                    dueEvent.DueElapsedMilliseconds,
                    processedEvents);
                if (contactDeath == DeathResolutionOutcome.SnakeTopologyUnavailable)
                {
                    return CreateTickFailure(
                        "SnakeTopologyUnavailable",
                        "物件效果後找不到合法安全蛇身。",
                        originalSnapshot);
                }

                if (contactDeath == DeathResolutionOutcome.ShieldRescued)
                {
                    shieldRescued = true;
                }

                if (snapshot.Game.Phase == Snake.Entities.GamePhase.Playing
                    && snapshot.Snake.Length >= snapshot.Level.TargetLength)
                {
                    CompleteCurrentLevel(snapshot, dueEvent.DueElapsedMilliseconds, processedEvents);
                }

                if (snapshot.Game.Phase == Snake.Entities.GamePhase.Playing)
                {
                    if (snapshot.DoorAndMaze.CloseDeferredDoors(snapshot.Snake.BodyCells))
                    {
                        processedEvents.Add(new GameTickEventDTO(
                            dueEvent.DueElapsedMilliseconds,
                            processedEvents.Count + 1,
                            GameTickEventType.DoorClosedAfterDeparture,
                            "Closed"));
                    }

                    ReplenishPendingObjects(
                        snapshot,
                        dueEvent.DueElapsedMilliseconds,
                        processedEvents);
                }
            }

            snapshot.Game.CommitTickTime(command.AccumulatedElapsedMilliseconds);
            if (!_snapshotStore.TryCommit(versionedSnapshot.Version, snapshot))
            {
                return CreateTickFailure(
                    "AtomicCommitFailed",
                    "完整 Tick 狀態提交失敗。",
                    originalSnapshot);
            }

            ProcessGameTickOutcome outcome = GetTickOutcome(snapshot, shieldRescued);
            RuntimeDirective runtimeDirective = snapshot.Game.Phase == Snake.Entities.GamePhase.Playing
                ? RuntimeDirective.Unchanged
                : RuntimeDirective.Stop;
            return new ProcessGameTickResult(
                true,
                outcome,
                null,
                null,
                _screenOutputFactory.Create(snapshot),
                runtimeDirective,
                processedEvents);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("ProcessGameTick workflow 執行失敗。", exception);
        }
    }

    private List<ScheduledGameEvent> CollectDueEvents(
        SnakeGameSnapshot snapshot,
        long accumulatedElapsedMilliseconds)
    {
        List<ScheduledGameEvent> events = new List<ScheduledGameEvent>();
        long lastProcessed = snapshot.Game.LastProcessedElapsedMilliseconds;
        long nextMovement = (lastProcessed / snapshot.Snake.MovementIntervalMilliseconds + 1)
            * snapshot.Snake.MovementIntervalMilliseconds;
        for (long due = nextMovement; due <= accumulatedElapsedMilliseconds; due += snapshot.Snake.MovementIntervalMilliseconds)
        {
            events.Add(new ScheduledGameEvent(due, GameTickEventType.SnakeMoved));
        }

        if (snapshot.Level.Number == Snake.Entities.LevelNumber.Third)
        {
            long toggleInterval = snapshot.DoorAndMaze.ToggleIntervalMilliseconds;
            long firstWarning = toggleInterval
                - snapshot.DoorAndMaze.ClosingWarningMilliseconds;
            for (long warning = firstWarning;
                warning <= accumulatedElapsedMilliseconds;
                warning += toggleInterval)
            {
                if (warning > lastProcessed)
                {
                    events.Add(new ScheduledGameEvent(warning, GameTickEventType.DoorClosingWarning));
                }
            }

            for (long toggle = toggleInterval;
                toggle <= accumulatedElapsedMilliseconds;
                toggle += toggleInterval)
            {
                if (toggle > lastProcessed)
                {
                    events.Add(new ScheduledGameEvent(toggle, GameTickEventType.DoorToggle));
                }
            }
        }

        return events
            .OrderBy(item => item.DueElapsedMilliseconds)
            .ThenBy(item => GetEventPriority(item.EventType))
            .ToList();
    }

    private int GetEventPriority(GameTickEventType eventType)
    {
        return eventType switch
        {
            GameTickEventType.DoorClosingWarning => 0,
            GameTickEventType.DoorToggle => 1,
            GameTickEventType.SnakeMoved => 2,
            _ => 3
        };
    }

    private List<Snake.Entities.GridPosition> GetTailBlockedCells(SnakeGameSnapshot snapshot)
    {
        List<Snake.Entities.GridPosition> blockedCells = snapshot.GetObjectOccupiedCells();
        blockedCells.AddRange(snapshot.DoorAndMaze.WallCells);
        blockedCells.AddRange(snapshot.DoorAndMaze.Doors
            .Where(door => door.Phase == Snake.Entities.DoorPhase.Closed)
            .Select(door => door.Position));
        return blockedCells;
    }

    private Snake.Entities.DeathCause? GetCollisionCause(
        SnakeGameSnapshot snapshot,
        Snake.Entities.GridPosition nextHead,
        Snake.Entities.SnakeMovementPlan plan)
    {
        if (plan.SelfCollisionCells.Contains(nextHead))
        {
            return Snake.Entities.DeathCause.SelfCollision;
        }

        if (snapshot.DoorAndMaze.WallCells.Contains(nextHead))
        {
            return Snake.Entities.DeathCause.WallCollision;
        }

        bool hitsClosedDoor = snapshot.DoorAndMaze.Doors.Any(door =>
            door.Position.Equals(nextHead) && door.Phase == Snake.Entities.DoorPhase.Closed);
        return hitsClosedDoor ? Snake.Entities.DeathCause.ClosedDoorCollision : null;
    }

    private DeathResolutionOutcome? ApplyObjectContact(
        SnakeGameSnapshot snapshot,
        Snake.Entities.GridPosition nextHead,
        Snake.Entities.FoodItem? contactedFood,
        long dueElapsedMilliseconds,
        List<GameTickEventDTO> processedEvents)
    {
        if (contactedFood is not null)
        {
            snapshot.Food.Consume(contactedFood.FoodItemId, contactedFood.Type);
            snapshot.Food.MarkPending(contactedFood.Type);
            AddObjectContactEvent(processedEvents, dueElapsedMilliseconds, contactedFood.Type.ToString());
            return null;
        }

        if (snapshot.Shield.ActiveItem is not null
            && snapshot.Shield.ActiveItem.Position.Equals(nextHead))
        {
            snapshot.Shield.Collect(snapshot.Shield.ActiveItem.ShieldItemId);
            snapshot.Shield.MarkPending();
            AddObjectContactEvent(processedEvents, dueElapsedMilliseconds, "Shield");
            return null;
        }

        if (snapshot.Fire.ActiveItem is not null
            && snapshot.Fire.ActiveItem.Position.Equals(nextHead))
        {
            Snake.Entities.FireItemId fireItemId = snapshot.Fire.ActiveItem.FireItemId;
            int preHitLength = snapshot.Snake.Length;
            snapshot.Fire.Consume(fireItemId);
            snapshot.Fire.MarkPending();
            Snake.Entities.LengthChangeResult lengthChange = snapshot.Snake.CalculateLengthChange(
                snapshot.Fire.LengthDelta);
            AddObjectContactEvent(processedEvents, dueElapsedMilliseconds, "Fire");
            if (!lengthChange.IsDepleted)
            {
                snapshot.Snake.CommitReduction(lengthChange);
                return null;
            }

            DeathResolutionOutcome outcome = ResolveDeath(
                snapshot,
                Snake.Entities.DeathCause.FatalFire,
                preHitLength);
            AddDeathEvent(processedEvents, dueElapsedMilliseconds, outcome);
            return outcome;
        }

        if (snapshot.Bomb.ActiveItem is not null
            && snapshot.Bomb.ActiveItem.Position.Equals(nextHead))
        {
            Snake.Entities.BombItemId bombItemId = snapshot.Bomb.ActiveItem.BombItemId;
            int preHitLength = snapshot.Snake.Length;
            snapshot.Bomb.Consume(bombItemId);
            snapshot.Bomb.MarkPending();
            AddObjectContactEvent(processedEvents, dueElapsedMilliseconds, "Bomb");
            DeathResolutionOutcome outcome = ResolveDeath(
                snapshot,
                Snake.Entities.DeathCause.Bomb,
                preHitLength);
            AddDeathEvent(processedEvents, dueElapsedMilliseconds, outcome);
            return outcome;
        }

        return null;
    }

    private DeathResolutionOutcome ResolveDeath(
        SnakeGameSnapshot snapshot,
        Snake.Entities.DeathCause deathCause,
        int preHitLength)
    {
        if (snapshot.Shield.CanRescue)
        {
            List<Snake.Entities.GridPosition> blockedCells = snapshot.GetObjectOccupiedCells();
            blockedCells.AddRange(snapshot.DoorAndMaze.WallCells);
            blockedCells.AddRange(snapshot.DoorAndMaze.Doors
                .Where(door => door.Phase == Snake.Entities.DoorPhase.Closed)
                .Select(door => door.Position));
            List<Snake.Entities.GridPosition>? safeBody = snapshot.Board.FindSafeBody(
                preHitLength,
                snapshot.Level.BoundaryPolicy,
                blockedCells);
            if (safeBody is null)
            {
                return DeathResolutionOutcome.SnakeTopologyUnavailable;
            }

            snapshot.Shield.UseCharge();
            snapshot.Snake.ResetSafely(
                safeBody,
                snapshot.Board,
                snapshot.Level.BoundaryPolicy);
            return DeathResolutionOutcome.ShieldRescued;
        }

        if (deathCause == Snake.Entities.DeathCause.FatalFire)
        {
            snapshot.Snake.Deplete();
        }

        snapshot.GameResult.Record(
            new Snake.Entities.LevelResultId(Guid.NewGuid()),
            snapshot.Level.Number,
            Snake.Entities.LevelOutcome.Failed,
            Math.Max(0, snapshot.Snake.Length),
            deathCause);
        snapshot.Game.EndGame();
        return DeathResolutionOutcome.GameOver;
    }

    private void CompleteCurrentLevel(
        SnakeGameSnapshot snapshot,
        long dueElapsedMilliseconds,
        List<GameTickEventDTO> processedEvents)
    {
        int completedLength = snapshot.Snake.Length;
        Snake.Entities.LevelNumber completedLevel = snapshot.Level.Number;
        snapshot.GameResult.Record(
            new Snake.Entities.LevelResultId(Guid.NewGuid()),
            completedLevel,
            Snake.Entities.LevelOutcome.Passed,
            completedLength,
            null);
        processedEvents.Add(new GameTickEventDTO(
            dueElapsedMilliseconds,
            processedEvents.Count + 1,
            GameTickEventType.LevelPassed,
            completedLevel.ToString()));
        if (completedLevel == Snake.Entities.LevelNumber.Third)
        {
            snapshot.Game.Complete();
            processedEvents.Add(new GameTickEventDTO(
                dueElapsedMilliseconds,
                processedEvents.Count + 1,
                GameTickEventType.GameCompleted,
                "Completed"));
            return;
        }

        snapshot.Level.Advance();
        snapshot.Food.Reset();
        snapshot.Shield.Reset();
        snapshot.Fire.Reset();
        snapshot.Bomb.Reset();
        snapshot.DoorAndMaze.Reset();
        if (snapshot.Level.Number == Snake.Entities.LevelNumber.Third)
        {
            snapshot.DoorAndMaze.PrepareThirdLevel(new Snake.Entities.DoorId(Guid.NewGuid()));
        }

        List<Snake.Entities.GridPosition> blockedCells = snapshot.DoorAndMaze.WallCells;
        blockedCells.AddRange(snapshot.DoorAndMaze.Doors.Select(door => door.Position));
        List<Snake.Entities.GridPosition>? safeBody = snapshot.Board.FindSafeBody(
            3,
            snapshot.Level.BoundaryPolicy,
            blockedCells);
        if (safeBody is null)
        {
            throw new InvalidOperationException("下一關沒有合法的三格安全蛇身。");
        }

        snapshot.Snake.ResetSafely(
            safeBody,
            snapshot.Board,
            snapshot.Level.BoundaryPolicy);
        snapshot.Game.PrepareNextLevel();
    }

    private void ReplenishPendingObjects(
        SnakeGameSnapshot snapshot,
        long dueElapsedMilliseconds,
        List<GameTickEventDTO> processedEvents)
    {
        foreach (Snake.Entities.FoodType foodType in snapshot.Food.PendingTypes)
        {
            Snake.Entities.GridPosition? position = SelectSpawnPosition(snapshot);
            if (position is null)
            {
                continue;
            }

            if (foodType == Snake.Entities.FoodType.Ordinary)
            {
                snapshot.Food.SpawnOrdinary(new Snake.Entities.FoodItemId(Guid.NewGuid()), position);
            }
            else
            {
                snapshot.Food.SpawnSpecial(new Snake.Entities.FoodItemId(Guid.NewGuid()), position);
            }

            snapshot.Food.RemovePending(foodType);
            AddReplenishedEvent(processedEvents, dueElapsedMilliseconds, foodType.ToString());
        }

        if (snapshot.Shield.PendingSpawn)
        {
            Snake.Entities.GridPosition? position = SelectSpawnPosition(snapshot);
            if (position is not null)
            {
                snapshot.Shield.Spawn(new Snake.Entities.ShieldItemId(Guid.NewGuid()), position);
                AddReplenishedEvent(processedEvents, dueElapsedMilliseconds, "Shield");
            }
        }

        if (snapshot.Fire.PendingSpawn)
        {
            Snake.Entities.GridPosition? position = SelectSpawnPosition(snapshot);
            if (position is not null)
            {
                snapshot.Fire.Spawn(new Snake.Entities.FireItemId(Guid.NewGuid()), position);
                AddReplenishedEvent(processedEvents, dueElapsedMilliseconds, "Fire");
            }
        }

        if (snapshot.Bomb.PendingSpawn)
        {
            Snake.Entities.GridPosition? position = SelectSpawnPosition(snapshot);
            if (position is not null)
            {
                snapshot.Bomb.Spawn(new Snake.Entities.BombItemId(Guid.NewGuid()), position);
                AddReplenishedEvent(processedEvents, dueElapsedMilliseconds, "Bomb");
            }
        }
    }

    private void AddObjectContactEvent(
        List<GameTickEventDTO> processedEvents,
        long dueElapsedMilliseconds,
        string outcome)
    {
        processedEvents.Add(new GameTickEventDTO(
            dueElapsedMilliseconds,
            processedEvents.Count + 1,
            GameTickEventType.ObjectContact,
            outcome));
    }

    private void AddDeathEvent(
        List<GameTickEventDTO> processedEvents,
        long dueElapsedMilliseconds,
        DeathResolutionOutcome outcome)
    {
        processedEvents.Add(new GameTickEventDTO(
            dueElapsedMilliseconds,
            processedEvents.Count + 1,
            outcome == DeathResolutionOutcome.ShieldRescued
                ? GameTickEventType.ShieldRescued
                : GameTickEventType.GameOver,
            outcome.ToString()));
    }

    private void AddReplenishedEvent(
        List<GameTickEventDTO> processedEvents,
        long dueElapsedMilliseconds,
        string outcome)
    {
        processedEvents.Add(new GameTickEventDTO(
            dueElapsedMilliseconds,
            processedEvents.Count + 1,
            GameTickEventType.ObjectReplenished,
            outcome));
    }

    private ProcessGameTickOutcome GetTickOutcome(
        SnakeGameSnapshot snapshot,
        bool shieldRescued)
    {
        if (snapshot.Game.Phase == Snake.Entities.GamePhase.GameOver)
        {
            return ProcessGameTickOutcome.GameOver;
        }

        if (snapshot.Game.Phase == Snake.Entities.GamePhase.Completed)
        {
            return ProcessGameTickOutcome.GameCompleted;
        }

        if (snapshot.Game.Phase == Snake.Entities.GamePhase.Ready)
        {
            return ProcessGameTickOutcome.LevelReady;
        }

        return shieldRescued
            ? ProcessGameTickOutcome.ShieldRescued
            : ProcessGameTickOutcome.EventsProcessed;
    }

    private ProcessGameTickResult CreateTickFailure(
        string failureCode,
        string failureMessage,
        SnakeGameSnapshot originalSnapshot)
    {
        return new ProcessGameTickResult(
            false,
            ProcessGameTickOutcome.Failed,
            failureCode,
            failureMessage,
            _screenOutputFactory.Create(originalSnapshot),
            RuntimeDirective.Unchanged,
            new List<GameTickEventDTO>());
    }

    private bool HasValidReadyState(SnakeGameSnapshot snapshot)
    {
        if (snapshot.Snake.Length != 3
            || snapshot.Shield.RemainingCharges != 0
            || snapshot.GetObjectOccupiedCells().Count != 0)
        {
            return false;
        }

        if (snapshot.Level.Number == Snake.Entities.LevelNumber.Third)
        {
            return snapshot.DoorAndMaze.WallCells.Count > 0
                && snapshot.DoorAndMaze.Doors.Count > 0;
        }

        return true;
    }

    private bool TrySpawnInitialObjects(SnakeGameSnapshot snapshot)
    {
        Snake.Entities.GridPosition? ordinaryPosition = SelectSpawnPosition(snapshot);
        if (ordinaryPosition is null)
        {
            return false;
        }

        snapshot.Food.SpawnOrdinary(
            new Snake.Entities.FoodItemId(Guid.NewGuid()),
            ordinaryPosition);
        if (snapshot.Level.Number == Snake.Entities.LevelNumber.First)
        {
            return true;
        }

        Snake.Entities.GridPosition? specialPosition = SelectSpawnPosition(snapshot);
        if (specialPosition is null)
        {
            return false;
        }

        snapshot.Food.SpawnSpecial(
            new Snake.Entities.FoodItemId(Guid.NewGuid()),
            specialPosition);
        Snake.Entities.GridPosition? shieldPosition = SelectSpawnPosition(snapshot);
        if (shieldPosition is null)
        {
            return false;
        }

        snapshot.Shield.Spawn(
            new Snake.Entities.ShieldItemId(Guid.NewGuid()),
            shieldPosition);
        Snake.Entities.GridPosition? firePosition = SelectSpawnPosition(snapshot);
        if (firePosition is null)
        {
            return false;
        }

        snapshot.Fire.Spawn(
            new Snake.Entities.FireItemId(Guid.NewGuid()),
            firePosition);
        if (snapshot.Level.Number != Snake.Entities.LevelNumber.Third)
        {
            return true;
        }

        Snake.Entities.GridPosition? bombPosition = SelectSpawnPosition(snapshot);
        if (bombPosition is null)
        {
            return false;
        }

        snapshot.Bomb.Spawn(
            new Snake.Entities.BombItemId(Guid.NewGuid()),
            bombPosition);
        return true;
    }

    private Snake.Entities.GridPosition? SelectSpawnPosition(SnakeGameSnapshot snapshot)
    {
        List<Snake.Entities.GridPosition> blockedCells = snapshot.GetObjectOccupiedCells();
        blockedCells.AddRange(snapshot.DoorAndMaze.WallCells);
        blockedCells.AddRange(snapshot.DoorAndMaze.Doors.Select(door => door.Position));
        List<Snake.Entities.GridPosition> legalCells = snapshot.Board.GetLegalSpawnCells(
            snapshot.Snake.BodyCells,
            blockedCells);
        List<GridPositionDTO> legalPositions = legalCells
            .Select(position => new GridPositionDTO(position.X, position.Y))
            .ToList();
        GridPositionDTO? selectedPosition = _spawnPositionSelector.Select(legalPositions);
        if (selectedPosition is null)
        {
            return null;
        }

        Snake.Entities.GridPosition selected = new Snake.Entities.GridPosition(
            selectedPosition.X,
            selectedPosition.Y);
        return legalCells.Contains(selected) ? selected : null;
    }

    private StartCurrentLevelResult CreateStartFailure(
        string failureCode,
        string failureMessage,
        SnakeGameSnapshot snapshot)
    {
        return new StartCurrentLevelResult(
            false,
            StartCurrentLevelOutcome.Failed,
            failureCode,
            failureMessage,
            _screenOutputFactory.Create(snapshot),
            RuntimeDirective.Unchanged);
    }

    private RestartGameResult CreateRestartFailure(
        string failureCode,
        string failureMessage,
        SnakeGameSnapshot snapshot)
    {
        return new RestartGameResult(
            false,
            RestartGameOutcome.Failed,
            failureCode,
            failureMessage,
            _screenOutputFactory.Create(snapshot),
            RuntimeDirective.Unchanged);
    }

    private Snake.Entities.Direction MapDirection(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Snake.Entities.Direction.Up,
            Direction.Down => Snake.Entities.Direction.Down,
            Direction.Left => Snake.Entities.Direction.Left,
            Direction.Right => Snake.Entities.Direction.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "不支援的方向。")
        };
    }
}
