using Snake.Application._EndCurrentLevelPlayUseCase;
using Snake.Application._EvaluateLevelCompletionUseCase;
using Snake.Application._GetGameScreenUseCase;
using Snake.Application._GetGameScreenUseCase.Enums;
using Snake.Application._GetGameScreenUseCase.Dto;
using Snake.Application._HandleBombContactUseCase;
using Snake.Application._HandleClosedDoorCollisionUseCase;
using Snake.Application._HandleFireContactUseCase;
using Snake.Application._HandleOrdinaryFoodContactUseCase;
using Snake.Application._HandleSelfCollisionUseCase;
using Snake.Application._HandleShieldContactUseCase;
using Snake.Application._HandleSpecialFoodContactUseCase;
using Snake.Application._HandleWallCollisionUseCase;
using Snake.Application._MoveSnakeUseCase;
using Snake.Application._PrepareNextLevelUseCase;
using Snake.Application._ReplenishTriggeredObjectUseCase;
using Snake.Application._RescueWithShieldUseCase;
using Snake.Application._RestartGameUseCase;
using Snake.Application._SetSnakeDirectionUseCase;
using Snake.Application._SetSnakeDirectionUseCase.Dto;
using Snake.Application._StartFirstLevelUseCase;
using Snake.Application._StartSecondLevelUseCase;
using Snake.Application._StartThirdLevelUseCase;
using Snake.Application._UpdateThirdLevelDoorStateUseCase;

namespace Snake.WinForms
{
    public partial class GameForm : Form
    {
        private StartFirstLevelUseCase _startFirstLevelUseCase;
        private StartSecondLevelUseCase _startSecondLevelUseCase;
        private StartThirdLevelUseCase _startThirdLevelUseCase;
        private SetSnakeDirectionUseCase _setSnakeDirectionUseCase;
        private UpdateThirdLevelDoorStateUseCase _updateThirdLevelDoorStateUseCase;
        private HandleSelfCollisionUseCase _handleSelfCollisionUseCase;
        private HandleWallCollisionUseCase _handleWallCollisionUseCase;
        private HandleClosedDoorCollisionUseCase _handleClosedDoorCollisionUseCase;
        private MoveSnakeUseCase _moveSnakeUseCase;
        private HandleOrdinaryFoodContactUseCase _handleOrdinaryFoodContactUseCase;
        private HandleSpecialFoodContactUseCase _handleSpecialFoodContactUseCase;
        private HandleShieldContactUseCase _handleShieldContactUseCase;
        private HandleFireContactUseCase _handleFireContactUseCase;
        private HandleBombContactUseCase _handleBombContactUseCase;
        private RescueWithShieldUseCase _rescueWithShieldUseCase;
        private EvaluateLevelCompletionUseCase _evaluateLevelCompletionUseCase;
        private EndCurrentLevelPlayUseCase _endCurrentLevelPlayUseCase;
        private PrepareNextLevelUseCase _prepareNextLevelUseCase;
        private ReplenishTriggeredObjectUseCase _replenishTriggeredObjectUseCase;
        private RestartGameUseCase _restartGameUseCase;
        private GetGameScreenUseCase _getGameScreenUseCase;
        private System.Windows.Forms.Timer _gameTimer;

        public GameForm(
            StartFirstLevelUseCase startFirstLevelUseCase,
            StartSecondLevelUseCase startSecondLevelUseCase,
            StartThirdLevelUseCase startThirdLevelUseCase,
            SetSnakeDirectionUseCase setSnakeDirectionUseCase,
            UpdateThirdLevelDoorStateUseCase updateThirdLevelDoorStateUseCase,
            HandleSelfCollisionUseCase handleSelfCollisionUseCase,
            HandleWallCollisionUseCase handleWallCollisionUseCase,
            HandleClosedDoorCollisionUseCase handleClosedDoorCollisionUseCase,
            MoveSnakeUseCase moveSnakeUseCase,
            HandleOrdinaryFoodContactUseCase handleOrdinaryFoodContactUseCase,
            HandleSpecialFoodContactUseCase handleSpecialFoodContactUseCase,
            HandleShieldContactUseCase handleShieldContactUseCase,
            HandleFireContactUseCase handleFireContactUseCase,
            HandleBombContactUseCase handleBombContactUseCase,
            RescueWithShieldUseCase rescueWithShieldUseCase,
            EvaluateLevelCompletionUseCase evaluateLevelCompletionUseCase,
            EndCurrentLevelPlayUseCase endCurrentLevelPlayUseCase,
            PrepareNextLevelUseCase prepareNextLevelUseCase,
            ReplenishTriggeredObjectUseCase replenishTriggeredObjectUseCase,
            RestartGameUseCase restartGameUseCase,
            GetGameScreenUseCase getGameScreenUseCase)
        {
            _startFirstLevelUseCase = startFirstLevelUseCase ?? throw new ArgumentNullException(nameof(startFirstLevelUseCase));
            _startSecondLevelUseCase = startSecondLevelUseCase ?? throw new ArgumentNullException(nameof(startSecondLevelUseCase));
            _startThirdLevelUseCase = startThirdLevelUseCase ?? throw new ArgumentNullException(nameof(startThirdLevelUseCase));
            _setSnakeDirectionUseCase = setSnakeDirectionUseCase ?? throw new ArgumentNullException(nameof(setSnakeDirectionUseCase));
            _updateThirdLevelDoorStateUseCase = updateThirdLevelDoorStateUseCase ?? throw new ArgumentNullException(nameof(updateThirdLevelDoorStateUseCase));
            _handleSelfCollisionUseCase = handleSelfCollisionUseCase ?? throw new ArgumentNullException(nameof(handleSelfCollisionUseCase));
            _handleWallCollisionUseCase = handleWallCollisionUseCase ?? throw new ArgumentNullException(nameof(handleWallCollisionUseCase));
            _handleClosedDoorCollisionUseCase = handleClosedDoorCollisionUseCase ?? throw new ArgumentNullException(nameof(handleClosedDoorCollisionUseCase));
            _moveSnakeUseCase = moveSnakeUseCase ?? throw new ArgumentNullException(nameof(moveSnakeUseCase));
            _handleOrdinaryFoodContactUseCase = handleOrdinaryFoodContactUseCase ?? throw new ArgumentNullException(nameof(handleOrdinaryFoodContactUseCase));
            _handleSpecialFoodContactUseCase = handleSpecialFoodContactUseCase ?? throw new ArgumentNullException(nameof(handleSpecialFoodContactUseCase));
            _handleShieldContactUseCase = handleShieldContactUseCase ?? throw new ArgumentNullException(nameof(handleShieldContactUseCase));
            _handleFireContactUseCase = handleFireContactUseCase ?? throw new ArgumentNullException(nameof(handleFireContactUseCase));
            _handleBombContactUseCase = handleBombContactUseCase ?? throw new ArgumentNullException(nameof(handleBombContactUseCase));
            _rescueWithShieldUseCase = rescueWithShieldUseCase ?? throw new ArgumentNullException(nameof(rescueWithShieldUseCase));
            _evaluateLevelCompletionUseCase = evaluateLevelCompletionUseCase ?? throw new ArgumentNullException(nameof(evaluateLevelCompletionUseCase));
            _endCurrentLevelPlayUseCase = endCurrentLevelPlayUseCase ?? throw new ArgumentNullException(nameof(endCurrentLevelPlayUseCase));
            _prepareNextLevelUseCase = prepareNextLevelUseCase ?? throw new ArgumentNullException(nameof(prepareNextLevelUseCase));
            _replenishTriggeredObjectUseCase = replenishTriggeredObjectUseCase ?? throw new ArgumentNullException(nameof(replenishTriggeredObjectUseCase));
            _restartGameUseCase = restartGameUseCase ?? throw new ArgumentNullException(nameof(restartGameUseCase));
            _getGameScreenUseCase = getGameScreenUseCase ?? throw new ArgumentNullException(nameof(getGameScreenUseCase));
            _gameTimer = new System.Windows.Forms.Timer
            {
                Interval = 250
            };

            Text = "貪吃蛇";
            ClientSize = new Size(1280, 860);
            MinimumSize = new Size(1000, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(251, 246, 232);
            Font = new Font("Microsoft JhengHei", 11, FontStyle.Regular, GraphicsUnit.Point);
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.Dpi;

            InitializeRendering();
            _startButton.Click += OnStartButtonClick;
            _replayButton.Click += OnReplayButtonClick;
            _gameTimer.Tick += OnGameTimerTick;
            DirectionRequested += OnDirectionRequested;
            RenderCurrentScreen();
        }

        public event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;

        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            SnakeDirectionDTO? direction = (keyData & Keys.KeyCode) switch
            {
                Keys.Up => new SnakeDirectionDTO(0, -1),
                Keys.Down => new SnakeDirectionDTO(0, 1),
                Keys.Left => new SnakeDirectionDTO(-1, 0),
                Keys.Right => new SnakeDirectionDTO(1, 0),
                _ => null
            };
            if (direction is null)
            {
                return base.ProcessCmdKey(ref message, keyData);
            }

            DirectionRequested?.Invoke(this, new DirectionRequestedEventArgs(direction));
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _gameTimer.Dispose();
                DisposeRenderingResources();
            }

            base.Dispose(disposing);
        }

        private void OnStartButtonClick(object? sender, EventArgs eventArgs)
        {
            RunUiAction(() =>
            {
                StartCurrentLevel();
                RenderCurrentScreen();
                _gameTimer.Start();
            });
        }

        private void OnReplayButtonClick(object? sender, EventArgs eventArgs)
        {
            RunUiAction(() =>
            {
                _restartGameUseCase.RestartGame();
                RenderCurrentScreen();
                _gameTimer.Start();
            });
        }

        private void OnDirectionRequested(object? sender, DirectionRequestedEventArgs eventArgs)
        {
            RunUiAction(() => _setSnakeDirectionUseCase.SetSnakeDirection(eventArgs.Direction));
        }

        private void OnGameTimerTick(object? sender, EventArgs eventArgs)
        {
            RunUiAction(() =>
            {
                ExecuteGameTick();
                GameScreenDTO screen = RenderCurrentScreen();
                if (!screen.AvailableActions.Contains(GameAction.ChangeDirection))
                {
                    _gameTimer.Stop();
                }
            });
        }

        private GameScreenDTO RenderCurrentScreen()
        {
            GameScreenDTO screen = _getGameScreenUseCase.GetGameScreen();
            ApplyScreen(screen);
            return screen;
        }

        private void StartCurrentLevel()
        {
            GameScreenDTO screen = _getGameScreenUseCase.GetGameScreen();
            switch (screen.LevelStatus.LevelNumber)
            {
                case 1:
                    _startFirstLevelUseCase.StartFirstLevel();
                    break;
                case 2:
                    _startSecondLevelUseCase.StartSecondLevel();
                    break;
                case 3:
                    _startThirdLevelUseCase.StartThirdLevel();
                    break;
                default:
                    throw new InvalidOperationException("目前關卡無法開始。");
            }
        }

        private void ExecuteGameTick()
        {
            _updateThirdLevelDoorStateUseCase.UpdateThirdLevelDoorState();
            _handleSelfCollisionUseCase.HandleSelfCollision();
            _handleWallCollisionUseCase.HandleWallCollision();
            _handleClosedDoorCollisionUseCase.HandleClosedDoorCollision();
            _moveSnakeUseCase.MoveSnake();
            _handleOrdinaryFoodContactUseCase.HandleOrdinaryFoodContact();
            _handleSpecialFoodContactUseCase.HandleSpecialFoodContact();
            _handleShieldContactUseCase.HandleShieldContact();
            _handleFireContactUseCase.HandleFireContact();
            _handleBombContactUseCase.HandleBombContact();
            _rescueWithShieldUseCase.RescueWithShield();
            _evaluateLevelCompletionUseCase.EvaluateLevelCompletion();
            _endCurrentLevelPlayUseCase.EndCurrentLevelPlay();
            _prepareNextLevelUseCase.PrepareNextLevel();
            _replenishTriggeredObjectUseCase.ReplenishTriggeredObject();
        }

        private void RunUiAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                _gameTimer.Stop();
                ShowFailure(exception.GetBaseException().Message);
            }
        }

    }
}

