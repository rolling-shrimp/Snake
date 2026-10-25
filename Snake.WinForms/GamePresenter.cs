using Snake.UseCases;

namespace Snake.WinForms;

public class GamePresenter : IFirstLevelPlayOutputPort, IAdvancedLevelPlayOutputPort
{
    private IGameView _view;
    private SnakeGameSession _session;
    private ShowFirstLevelReadyBoardUseCase _firstReadyBoardUseCase;
    private ShowFirstLevelRulesUseCase _firstRulesUseCase;
    private ShowFirstLevelStatusUseCase _firstStatusUseCase;
    private ShowSecondLevelReadyBoardUseCase _secondReadyBoardUseCase;
    private ShowSecondLevelRulesUseCase _secondRulesUseCase;
    private ShowSecondLevelStatusUseCase _secondStatusUseCase;
    private ShowThirdLevelReadyBoardUseCase _thirdReadyBoardUseCase;
    private ShowThirdLevelRulesUseCase _thirdRulesUseCase;
    private ShowThirdLevelStatusUseCase _thirdStatusUseCase;
    private ShowThirdLevelDoorStatusUseCase _thirdDoorStatusUseCase;
    private ShowLevelPassedResultUseCase _levelPassedResultUseCase;
    private StartFirstLevelUseCase _startFirstLevelUseCase = null!;
    private StartSecondLevelUseCase _startSecondLevelUseCase = null!;
    private StartThirdLevelUseCase _startThirdLevelUseCase = null!;
    private SetSnakeDirectionUseCase _setSnakeDirectionUseCase = null!;
    private AdvanceSnakeStepUseCase _advanceSnakeStepUseCase = null!;
    private HandleOrdinaryFoodContactUseCase _handleOrdinaryFoodContactUseCase = null!;
    private HandleSpecialFoodContactUseCase _handleSpecialFoodContactUseCase = null!;
    private HandleShieldContactUseCase _handleShieldContactUseCase = null!;
    private HandleFireContactUseCase _handleFireContactUseCase = null!;
    private HandleBombContactUseCase _handleBombContactUseCase = null!;
    private HandleSelfCollisionUseCase _handleSelfCollisionUseCase = null!;
    private HandleWallCollisionUseCase _handleWallCollisionUseCase = null!;
    private HandleClosedDoorCollisionUseCase _handleClosedDoorCollisionUseCase = null!;
    private ReplenishTriggeredObjectUseCase _replenishTriggeredObjectUseCase = null!;
    private EvaluateLevelCompletionUseCase _evaluateLevelCompletionUseCase = null!;
    private EndCurrentLevelPlayUseCase _endCurrentLevelPlayUseCase = null!;
    private RescueWithShieldUseCase _rescueWithShieldUseCase = null!;
    private UpdateThirdLevelDoorStateUseCase _updateThirdLevelDoorStateUseCase = null!;
    private RestartGameUseCase _restartGameUseCase = null!;
    private ShowGameOverResultUseCase _gameOverResultUseCase;
    private ShowGameCompletedResultUseCase _gameCompletedResultUseCase;
    private UpdateLevelStatusReadModelUseCase _updateLevelStatusUseCase;
    private Dictionary<int, List<RuleHintViewModel>> _levelRuleHints;
    private long _statusVersion;
    private int _doorCycleElapsedMilliseconds;

    public GamePresenter(IGameView view, SnakeGameSession session)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _firstReadyBoardUseCase = new ShowFirstLevelReadyBoardUseCase(
            session,
            new FirstLevelReadyBoardProjection());
        _firstRulesUseCase = new ShowFirstLevelRulesUseCase(
            session,
            new FirstLevelRulesProjection());
        _firstStatusUseCase = new ShowFirstLevelStatusUseCase(
            session,
            new FirstLevelStatusProjection());
        _secondReadyBoardUseCase = new ShowSecondLevelReadyBoardUseCase(session);
        _secondRulesUseCase = new ShowSecondLevelRulesUseCase(session);
        _secondStatusUseCase = new ShowSecondLevelStatusUseCase(session);
        _thirdReadyBoardUseCase = new ShowThirdLevelReadyBoardUseCase(session);
        _thirdRulesUseCase = new ShowThirdLevelRulesUseCase(session);
        _thirdStatusUseCase = new ShowThirdLevelStatusUseCase(session);
        _thirdDoorStatusUseCase = new ShowThirdLevelDoorStatusUseCase(session);
        _levelPassedResultUseCase = new ShowLevelPassedResultUseCase(session);
        _gameOverResultUseCase = new ShowGameOverResultUseCase(session);
        _gameCompletedResultUseCase = new ShowGameCompletedResultUseCase(session);
        _updateLevelStatusUseCase = new UpdateLevelStatusReadModelUseCase(session);
        _levelRuleHints = new Dictionary<int, List<RuleHintViewModel>>();
        _statusVersion = 0;
        _doorCycleElapsedMilliseconds = 0;
        BuildRoundUseCases();
        _view.StartRequested += OnStartRequested;
        _view.ReplayRequested += OnReplayRequested;
        _view.DirectionRequested += OnDirectionRequested;
        _view.TickRequested += OnTickRequested;
    }

    public void Initialize()
    {
        try
        {
            RenderFirstLevelReady();
        }
        catch (Exception exception)
        {
            _view.ShowFailure($"初始化第一關畫面失敗：{exception.Message}");
        }
    }

    public void HideStartButton()
    {
    }

    public void ShowOrdinaryFood(FirstLevelFoodDTO food)
    {
        ArgumentNullException.ThrowIfNull(food);
    }

    public void HideReadyState(int levelNumber)
    {
    }

    public void ShowObject(GameObjectType objectType, GridPositionDTO position)
    {
        ArgumentNullException.ThrowIfNull(position);
    }

    public void StartMovement(FirstLevelMovementDTO movement)
    {
        ArgumentNullException.ThrowIfNull(movement);
        RenderPlaying(_session.ReadGameplay());
    }

    public void StartDoorCycle(int toggleIntervalMilliseconds, int warningMilliseconds)
    {
    }

    private void RenderFirstLevelReady()
    {
        ShowFirstLevelReadyBoardResult boardResult = _firstReadyBoardUseCase.Execute(
            new ShowFirstLevelReadyBoardQuery());
        ShowFirstLevelRulesResult rulesResult = _firstRulesUseCase.Execute(
            new ShowFirstLevelRulesQuery());
        ShowFirstLevelStatusResult statusResult = _firstStatusUseCase.Execute(
            new ShowFirstLevelStatusQuery());
        if (!boardResult.IsSuccess || boardResult.Data is null)
        {
            throw new InvalidOperationException(boardResult.FailureMessage ?? "無法顯示第一關棋盤。");
        }

        if (!rulesResult.IsSuccess || rulesResult.Data is null)
        {
            throw new InvalidOperationException(rulesResult.FailureMessage ?? "無法顯示第一關規則。");
        }

        if (!statusResult.IsSuccess || statusResult.Data is null)
        {
            throw new InvalidOperationException(statusResult.FailureMessage ?? "無法顯示第一關狀態。");
        }

        List<RuleHintViewModel> ruleHints = rulesResult.Data.RuleHints
            .Select(rule => new RuleHintViewModel(rule.Code, rule.Text))
            .ToList();
        _levelRuleHints[1] = ruleHints;
        GameViewModel viewModel = new GameViewModel(
            "first-level-ready",
            new GameBoardViewModel(
                true,
                boardResult.Data.BoardRows,
                boardResult.Data.BoardColumns,
                boardResult.Data.SnakeCells.Select(ToGridCell).ToList(),
                new List<GridCellViewModel>(),
                new List<DoorViewModel>(),
                new List<BoardObjectViewModel>()),
            new GameStatusViewModel(
                statusResult.Data.LevelName,
                statusResult.Data.CurrentLength,
                statusResult.Data.TargetLength,
                statusResult.Data.ProgressPercent,
                null),
            ruleHints,
            boardResult.Data.IsStartButtonVisible,
            false,
            false,
            0,
            null);
        Render(viewModel);
    }

    private void RenderPlaying(GameplayReadModel snapshot)
    {
        if (!_levelRuleHints.TryGetValue(snapshot.LevelNumber, out List<RuleHintViewModel>? ruleHints))
        {
            throw new InvalidOperationException($"第 {snapshot.LevelNumber} 關規則尚未載入。");
        }

        _statusVersion++;
        _updateLevelStatusUseCase.Execute(new UpdateLevelStatusReadModelCommand(
            Guid.NewGuid().ToString(),
            _statusVersion));
        LevelStatusReadModel status = _updateLevelStatusUseCase.Current
            ?? throw new InvalidOperationException("遊玩狀態尚未建立。");
        GameViewModel viewModel = new GameViewModel(
            $"{GetLevelKey(snapshot.LevelNumber)}-level-playing",
            ToBoardViewModel(snapshot, true),
            new GameStatusViewModel(
                status.LevelName,
                status.CurrentLength,
                status.TargetLength,
                status.ProgressPercent,
                status.ShieldRemainingCharges),
            ruleHints,
            false,
            false,
            snapshot.IsMovementActive,
            snapshot.MovementIntervalMilliseconds,
            null);
        Render(viewModel);
    }

    private void RenderSecondLevelReady()
    {
        ShowSecondLevelReadyBoardResult boardResult = _secondReadyBoardUseCase.Execute(
            new ShowSecondLevelReadyBoardQuery());
        ShowSecondLevelRulesResult rulesResult = _secondRulesUseCase.Execute(
            new ShowSecondLevelRulesQuery());
        ShowSecondLevelStatusResult statusResult = _secondStatusUseCase.Execute(
            new ShowSecondLevelStatusQuery());
        LevelPassedResultRspDTO passedResult = ReadPassedResult();
        if (!boardResult.IsSuccess || boardResult.Data is null)
        {
            throw new InvalidOperationException(boardResult.FailureMessage ?? "無法顯示第二關棋盤。");
        }

        if (!rulesResult.IsSuccess || rulesResult.Data is null)
        {
            throw new InvalidOperationException(rulesResult.FailureMessage ?? "無法顯示第二關規則。");
        }

        if (!statusResult.IsSuccess || statusResult.Data is null)
        {
            throw new InvalidOperationException(statusResult.FailureMessage ?? "無法顯示第二關狀態。");
        }

        List<RuleHintViewModel> ruleHints = rulesResult.Data.RuleHints
            .Select(rule => new RuleHintViewModel(rule.Code, rule.Text))
            .ToList();
        _levelRuleHints[2] = ruleHints;
        Render(new GameViewModel(
            "second-level-ready",
            new GameBoardViewModel(
                true,
                boardResult.Data.BoardRows,
                boardResult.Data.BoardColumns,
                boardResult.Data.SnakeCells.Select(ToGridCell).ToList(),
                new List<GridCellViewModel>(),
                new List<DoorViewModel>(),
                new List<BoardObjectViewModel>()),
            new GameStatusViewModel(
                statusResult.Data.LevelName,
                statusResult.Data.CurrentLength,
                statusResult.Data.TargetLength,
                statusResult.Data.ProgressPercent,
                statusResult.Data.ShieldRemainingCharges),
            ruleHints,
            boardResult.Data.IsStartButtonVisible,
            false,
            false,
            0,
            ToPassedResultCard(passedResult)));
    }

    private void RenderThirdLevelReady()
    {
        ShowThirdLevelReadyBoardResult boardResult = _thirdReadyBoardUseCase.Execute(
            new ShowThirdLevelReadyBoardQuery());
        ShowThirdLevelRulesResult rulesResult = _thirdRulesUseCase.Execute(
            new ShowThirdLevelRulesQuery());
        ShowThirdLevelStatusResult statusResult = _thirdStatusUseCase.Execute(
            new ShowThirdLevelStatusQuery());
        ShowThirdLevelDoorStatusResult doorResult = _thirdDoorStatusUseCase.Execute(
            new ShowThirdLevelDoorStatusQuery());
        LevelPassedResultRspDTO passedResult = ReadPassedResult();
        if (!boardResult.IsSuccess || boardResult.Data is null)
        {
            throw new InvalidOperationException(boardResult.FailureMessage ?? "無法顯示第三關棋盤。");
        }

        if (!rulesResult.IsSuccess || rulesResult.Data is null)
        {
            throw new InvalidOperationException(rulesResult.FailureMessage ?? "無法顯示第三關規則。");
        }

        if (!statusResult.IsSuccess || statusResult.Data is null)
        {
            throw new InvalidOperationException(statusResult.FailureMessage ?? "無法顯示第三關狀態。");
        }

        if (!doorResult.IsSuccess || doorResult.Data is null)
        {
            throw new InvalidOperationException(doorResult.FailureMessage ?? "無法顯示第三關門狀態。");
        }

        List<RuleHintViewModel> ruleHints = rulesResult.Data.RuleHints
            .Select(rule => new RuleHintViewModel(rule.Code, rule.Text))
            .ToList();
        _levelRuleHints[3] = ruleHints;
        Render(new GameViewModel(
            "third-level-ready",
            new GameBoardViewModel(
                true,
                boardResult.Data.BoardRows,
                boardResult.Data.BoardColumns,
                boardResult.Data.SnakeCells.Select(ToGridCell).ToList(),
                boardResult.Data.WallCells.Select(ToGridCell).ToList(),
                doorResult.Data.Doors.Select(door => new DoorViewModel(
                    door.DoorId,
                    ToGridCell(door.Position),
                    door.State)).ToList(),
                new List<BoardObjectViewModel>()),
            new GameStatusViewModel(
                statusResult.Data.LevelName,
                statusResult.Data.CurrentLength,
                statusResult.Data.TargetLength,
                statusResult.Data.ProgressPercent,
                statusResult.Data.ShieldRemainingCharges),
            ruleHints,
            boardResult.Data.IsStartButtonVisible,
            false,
            false,
            0,
            ToPassedResultCard(passedResult)));
    }

    private LevelPassedResultRspDTO ReadPassedResult()
    {
        ShowLevelPassedResultResult result = _levelPassedResultUseCase.Execute(
            new ShowLevelPassedResultQuery());
        if (!result.IsSuccess || result.Data is null)
        {
            throw new InvalidOperationException(result.FailureMessage ?? "無法顯示上一關成績。");
        }

        return result.Data;
    }

    private GameResultCardViewModel ToPassedResultCard(LevelPassedResultRspDTO passedResult)
    {
        return new GameResultCardViewModel(
            $"第 {passedResult.CompletedLevel} 關過關",
            passedResult.BannerText,
            passedResult.CompletedLevel,
            passedResult.FinalLength,
            $"通關蛇長：{passedResult.FinalLength} 格");
    }

    private void RenderCurrentScreen()
    {
        GameplayReadModel snapshot = _session.ReadGameplay();
        if (snapshot.Phase == GameplayPhase.Playing)
        {
            RenderPlaying(snapshot);
            return;
        }

        if (snapshot.Phase == GameplayPhase.Ready && snapshot.LevelNumber == 2)
        {
            RenderSecondLevelReady();
            return;
        }

        if (snapshot.Phase == GameplayPhase.Ready && snapshot.LevelNumber == 3)
        {
            RenderThirdLevelReady();
            return;
        }

        if (snapshot.Phase == GameplayPhase.GameOver)
        {
            RenderGameOver();
            return;
        }

        if (snapshot.Phase == GameplayPhase.Completed)
        {
            RenderGameCompleted(snapshot);
            return;
        }

        throw new InvalidOperationException("目前畫面狀態尚未支援。");
    }

    private void RenderGameOver()
    {
        ShowGameOverResultResult result = _gameOverResultUseCase.Execute(
            new ShowGameOverResultQuery());
        if (!result.IsSuccess || result.Data is null)
        {
            throw new InvalidOperationException(result.FailureMessage ?? "無法顯示失敗結果。");
        }

        int targetLength = result.Data.FailedLevel == 3 ? 50 : 20;
        Render(new GameViewModel(
            "game-over",
            new GameBoardViewModel(
                false,
                10,
                10,
                new List<GridCellViewModel>(),
                new List<GridCellViewModel>(),
                new List<DoorViewModel>(),
                new List<BoardObjectViewModel>()),
            new GameStatusViewModel(
                GetLevelName(result.Data.FailedLevel),
                result.Data.FinalLength,
                targetLength,
                result.Data.ProgressPercent,
                null),
            new List<RuleHintViewModel>(),
            false,
            result.Data.IsReplayButtonVisible,
            false,
            0,
            new GameResultCardViewModel(
                "Game Over",
                "遊戲結束",
                result.Data.FailedLevel,
                result.Data.FinalLength,
                GetDeathCauseText(result.Data.DeathCause))));
    }

    private void RenderGameCompleted(GameplayReadModel snapshot)
    {
        ShowGameCompletedResultResult result = _gameCompletedResultUseCase.Execute(
            new ShowGameCompletedResultQuery());
        if (!result.IsSuccess || result.Data is null)
        {
            throw new InvalidOperationException(result.FailureMessage ?? "無法顯示完成結果。");
        }

        Render(new GameViewModel(
            "game-completed",
            ToBoardViewModel(snapshot, true),
            new GameStatusViewModel(
                GetLevelName(snapshot.LevelNumber),
                result.Data.FinalLength,
                snapshot.TargetLength,
                result.Data.ProgressPercent,
                snapshot.ShieldRemainingCharges),
            new List<RuleHintViewModel>(),
            false,
            result.Data.IsReplayButtonVisible,
            false,
            0,
            new GameResultCardViewModel(
                result.Data.SuccessMessage,
                "三關全部完成",
                3,
                result.Data.FinalLength,
                $"最終蛇長：{result.Data.FinalLength} 格")));
    }

    private GameBoardViewModel ToBoardViewModel(GameplayReadModel snapshot, bool isVisible)
    {
        return new GameBoardViewModel(
            isVisible,
            snapshot.BoardRows,
            snapshot.BoardColumns,
            snapshot.SnakeCells.Select(ToGridCell).ToList(),
            snapshot.WallCells.Select(ToGridCell).ToList(),
            snapshot.Doors.Select(door => new DoorViewModel(
                door.DoorId,
                ToGridCell(door.Position),
                door.State)).ToList(),
            snapshot.Objects.Select(gameObject => new BoardObjectViewModel(
                gameObject.Type,
                gameObject.ItemId,
                ToGridCell(gameObject.Position))).ToList());
    }

    private GridCellViewModel ToGridCell(GridPositionDTO position)
    {
        return new GridCellViewModel(position.X, position.Y);
    }

    private string GetLevelKey(int levelNumber)
    {
        return levelNumber switch
        {
            1 => "first",
            2 => "second",
            3 => "third",
            _ => throw new InvalidOperationException($"不支援的關卡編號：{levelNumber}。")
        };
    }

    private void Render(GameViewModel viewModel)
    {
        _view.Render(viewModel);
    }

    private void OnStartRequested(object? sender, EventArgs eventArgs)
    {
        try
        {
            GameplayReadModel snapshot = _session.ReadGameplay();
            if (snapshot.LevelNumber == 1)
            {
                StartFirstLevelResult result = _startFirstLevelUseCase.Execute(new StartFirstLevelCommand());
                ShowFailure(result.IsSuccess, result.FailureMessage, "目前無法開始第一關。");
            }
            else if (snapshot.LevelNumber == 2)
            {
                StartSecondLevelResult result = _startSecondLevelUseCase.Execute(new StartSecondLevelCommand());
                ShowFailure(result.IsSuccess, result.FailureMessage, "目前無法開始第二關。");
            }
            else if (snapshot.LevelNumber == 3)
            {
                StartThirdLevelResult result = _startThirdLevelUseCase.Execute(new StartThirdLevelCommand());
                ShowFailure(result.IsSuccess, result.FailureMessage, "目前無法開始第三關。");
            }
        }
        catch (Exception exception)
        {
            _view.ShowFailure($"開始第一關失敗：{exception.Message}");
        }
    }

    private void OnReplayRequested(object? sender, EventArgs eventArgs)
    {
        try
        {
            RestartGameResult result = _restartGameUseCase.Execute(new RestartGameCommand());
            ShowFailure(result.IsSuccess, result.FailureMessage, "目前無法重新開始遊戲。");
            if (result.IsSuccess && _session.ReadGameplay().Phase == GameplayPhase.Playing)
            {
                BuildRoundUseCases();
            }
        }
        catch (Exception exception)
        {
            _view.ShowFailure($"重新開始遊戲失敗：{exception.Message}");
        }
    }

    private void OnDirectionRequested(object? sender, DirectionRequestedEventArgs eventArgs)
    {
        try
        {
            _setSnakeDirectionUseCase.Execute(new SetSnakeDirectionCommand(eventArgs.Direction));
        }
        catch (Exception exception)
        {
            _view.ShowFailure($"變更方向失敗：{exception.Message}");
        }
    }

    private void OnTickRequested(object? sender, GameTickEventArgs eventArgs)
    {
        try
        {
            UpdateThirdLevelDoorState(eventArgs.ElapsedMilliseconds);
            _advanceSnakeStepUseCase.Execute(new AdvanceSnakeStepCommand(eventArgs.ElapsedMilliseconds));
            GameplayReadModel snapshot = _session.ReadGameplay();
            if (snapshot.Phase != GameplayPhase.Playing)
            {
                RenderCurrentScreen();
                return;
            }

            if (snapshot.LastMovementCollision is not null)
            {
                HandleMovementCollision(snapshot.LastMovementCollision, snapshot.CurrentLength);
                RenderCurrentScreen();
                return;
            }

            GameObjectStateDTO? contactedObject = HandleObjectContact(snapshot);

            _evaluateLevelCompletionUseCase.Execute(new EvaluateLevelCompletionCommand());
            GameplayReadModel updatedSnapshot = _session.ReadGameplay();
            if (updatedSnapshot.CurrentLength >= updatedSnapshot.TargetLength)
            {
                _endCurrentLevelPlayUseCase.Execute(new EndCurrentLevelPlayCommand(
                    LevelOutcome.Passed,
                    null));
            }

            if (contactedObject is not null
                && _session.ReadGameplay().Phase == GameplayPhase.Playing)
            {
                _replenishTriggeredObjectUseCase.Execute(new ReplenishTriggeredObjectCommand(
                    contactedObject.Type,
                    contactedObject.ItemId));
            }

            RenderCurrentScreen();
        }
        catch (Exception exception)
        {
            _view.ShowFailure($"更新遊戲失敗：{exception.Message}");
        }
    }

    private GameObjectStateDTO? HandleObjectContact(GameplayReadModel snapshot)
    {
        if (snapshot.SnakeCells.Count == 0)
        {
            return null;
        }

        GameObjectStateDTO? contactedObject = snapshot.Objects.FirstOrDefault(gameObject =>
            IsSamePosition(gameObject.Position, snapshot.SnakeCells[0]));
        if (contactedObject is null)
        {
            return null;
        }

        int preHitLength = snapshot.CurrentLength;
        switch (contactedObject.Type)
        {
            case GameObjectType.Strawberry:
                _handleOrdinaryFoodContactUseCase.Execute(
                    new HandleOrdinaryFoodContactCommand(contactedObject.ItemId));
                break;
            case GameObjectType.SpecialFood:
                _handleSpecialFoodContactUseCase.Execute(
                    new HandleSpecialFoodContactCommand(contactedObject.ItemId));
                break;
            case GameObjectType.Shield:
                _handleShieldContactUseCase.Execute(
                    new HandleShieldContactCommand(contactedObject.ItemId));
                break;
            case GameObjectType.Fire:
                _handleFireContactUseCase.Execute(
                    new HandleFireContactCommand(contactedObject.ItemId));
                break;
            case GameObjectType.Bomb:
                _handleBombContactUseCase.Execute(
                    new HandleBombContactCommand(contactedObject.ItemId));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(contactedObject.Type),
                    contactedObject.Type,
                    "不支援的遊戲物件。");
        }

        if (contactedObject.Type == GameObjectType.Bomb)
        {
            RescueOrEnd(DeathCause.Bomb, preHitLength);
        }
        else if (contactedObject.Type == GameObjectType.Fire && preHitLength <= 2)
        {
            RescueOrEnd(DeathCause.FatalFire, preHitLength);
        }

        return contactedObject;
    }

    private void HandleMovementCollision(GameplayCollisionDTO collision, int preHitLength)
    {
        DeathCause deathCause;
        switch (collision.Type)
        {
            case GameplayCollisionType.SelfCollision:
                _handleSelfCollisionUseCase.Execute(
                    new HandleSelfCollisionCommand(collision.Position));
                deathCause = DeathCause.SelfCollision;
                break;
            case GameplayCollisionType.WallCollision:
                _handleWallCollisionUseCase.Execute(
                    new HandleWallCollisionCommand(collision.Position));
                deathCause = DeathCause.WallCollision;
                break;
            case GameplayCollisionType.ClosedDoorCollision:
                if (string.IsNullOrWhiteSpace(collision.DoorId))
                {
                    throw new InvalidOperationException("關門碰撞缺少門識別碼。");
                }

                _handleClosedDoorCollisionUseCase.Execute(
                    new HandleClosedDoorCollisionCommand(collision.DoorId));
                deathCause = DeathCause.ClosedDoorCollision;
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(collision.Type),
                    collision.Type,
                    "不支援的碰撞類型。");
        }

        RescueOrEnd(deathCause, preHitLength);
    }

    private void RescueOrEnd(DeathCause deathCause, int preHitLength)
    {
        _rescueWithShieldUseCase.Execute(new RescueWithShieldCommand(deathCause, preHitLength));
        if (_session.ReadGameplay().Phase == GameplayPhase.Playing)
        {
            BuildDeathUseCases();
        }
    }

    private void UpdateThirdLevelDoorState(int elapsedMilliseconds)
    {
        GameplayReadModel snapshot = _session.ReadGameplay();
        if (snapshot.Phase != GameplayPhase.Playing || snapshot.LevelNumber != 3)
        {
            return;
        }

        _doorCycleElapsedMilliseconds += elapsedMilliseconds;
        _updateThirdLevelDoorStateUseCase.Execute(new UpdateThirdLevelDoorStateCommand(
            _doorCycleElapsedMilliseconds));
        if (_doorCycleElapsedMilliseconds >= snapshot.DoorToggleIntervalMilliseconds)
        {
            _doorCycleElapsedMilliseconds = 0;
        }
    }

    private void BuildRoundUseCases()
    {
        ActivateFirstLevelPlayUseCase activateFirstUseCase = new ActivateFirstLevelPlayUseCase(
            _session,
            this);
        _startFirstLevelUseCase = new StartFirstLevelUseCase(
            _session,
            new StartFirstLevelAcceptedReactor(activateFirstUseCase));
        ActivateSecondLevelPlayUseCase activateSecondUseCase = new ActivateSecondLevelPlayUseCase(
            _session,
            this);
        _startSecondLevelUseCase = new StartSecondLevelUseCase(
            _session,
            new StartSecondLevelAcceptedReactor(activateSecondUseCase));
        ActivateThirdLevelPlayUseCase activateThirdUseCase = new ActivateThirdLevelPlayUseCase(
            _session,
            this);
        _startThirdLevelUseCase = new StartThirdLevelUseCase(
            _session,
            new StartThirdLevelAcceptedReactor(activateThirdUseCase));
        _setSnakeDirectionUseCase = new SetSnakeDirectionUseCase(_session);
        _advanceSnakeStepUseCase = new AdvanceSnakeStepUseCase(_session);
        _handleOrdinaryFoodContactUseCase = new HandleOrdinaryFoodContactUseCase(_session);
        _handleSpecialFoodContactUseCase = new HandleSpecialFoodContactUseCase(_session);
        _handleShieldContactUseCase = new HandleShieldContactUseCase(_session);
        _handleFireContactUseCase = new HandleFireContactUseCase(_session);
        _handleBombContactUseCase = new HandleBombContactUseCase(_session);
        _replenishTriggeredObjectUseCase = new ReplenishTriggeredObjectUseCase(_session);
        _evaluateLevelCompletionUseCase = new EvaluateLevelCompletionUseCase(_session);
        PrepareNextLevelUseCase prepareNextLevelUseCase = new PrepareNextLevelUseCase(_session);
        _endCurrentLevelPlayUseCase = new EndCurrentLevelPlayUseCase(
            _session,
            prepareNextLevelUseCase);
        _rescueWithShieldUseCase = new RescueWithShieldUseCase(
            _session,
            _endCurrentLevelPlayUseCase);
        _updateThirdLevelDoorStateUseCase = new UpdateThirdLevelDoorStateUseCase(_session);
        ResetGameForReplayUseCase resetGameForReplayUseCase = new ResetGameForReplayUseCase(
            _session,
            this);
        _restartGameUseCase = new RestartGameUseCase(
            _session,
            new RestartGameAcceptedReactor(resetGameForReplayUseCase));
        _doorCycleElapsedMilliseconds = 0;
        BuildDeathUseCases();
    }

    private void BuildDeathUseCases()
    {
        _handleSelfCollisionUseCase = new HandleSelfCollisionUseCase(_session);
        _handleWallCollisionUseCase = new HandleWallCollisionUseCase(_session);
        _handleClosedDoorCollisionUseCase = new HandleClosedDoorCollisionUseCase(_session);
        _rescueWithShieldUseCase = new RescueWithShieldUseCase(
            _session,
            _endCurrentLevelPlayUseCase);
    }

    private string GetLevelName(int levelNumber)
    {
        return levelNumber switch
        {
            1 => "第一關",
            2 => "第二關",
            3 => "第三關",
            _ => throw new InvalidOperationException($"不支援的關卡編號：{levelNumber}。")
        };
    }

    private string GetDeathCauseText(DeathCause deathCause)
    {
        return deathCause switch
        {
            DeathCause.SelfCollision => "撞到自己的身體",
            DeathCause.WallCollision => "撞到牆壁",
            DeathCause.ClosedDoorCollision => "撞到關閉的門",
            DeathCause.Bomb => "碰到炸彈",
            DeathCause.FatalFire => "火焰使蛇身歸零",
            _ => throw new ArgumentOutOfRangeException(nameof(deathCause), deathCause, "不支援的死亡原因。")
        };
    }

    private bool IsSamePosition(GridPositionDTO left, GridPositionDTO right)
    {
        return left.X == right.X && left.Y == right.Y;
    }

    private void ShowFailure(bool isSuccess, string? failureMessage, string fallbackMessage)
    {
        if (!isSuccess)
        {
            _view.ShowFailure(failureMessage ?? fallbackMessage);
        }
    }
}
