using Snake.UseCases;

namespace Snake.WinForms;

public class GamePresenter
{
    private IGameView _view;
    private ISnakeGameApplicationService _applicationService;

    public GamePresenter(
        IGameView view,
        ISnakeGameApplicationService applicationService)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _applicationService = applicationService
            ?? throw new ArgumentNullException(nameof(applicationService));
        _view.StartRequested += OnStartRequested;
        _view.ReplayRequested += OnReplayRequested;
        _view.DirectionRequested += OnDirectionRequested;
        _view.TickRequested += OnTickRequested;
    }

    public void Initialize()
    {
        try
        {
            GetCurrentScreenResult result = _applicationService.GetCurrentScreen(
                new GetCurrentScreenQuery());
            ApplyResult(
                result.IsSuccess,
                result.FailureMessage,
                result.Screen,
                result.RuntimeDirective);
        }
        catch (Exception exception)
        {
            _view.ShowFailure($"初始化畫面失敗：{exception.Message}");
        }
    }

    private void OnStartRequested(object? sender, EventArgs eventArgs)
    {
        try
        {
            StartCurrentLevelResult result = _applicationService.StartCurrentLevel(
                new StartCurrentLevelCommand());
            ApplyResult(
                result.IsSuccess,
                result.FailureMessage,
                result.Screen,
                result.RuntimeDirective);
        }
        catch (Exception exception)
        {
            _view.ShowFailure($"開始關卡失敗：{exception.Message}");
        }
    }

    private void OnReplayRequested(object? sender, EventArgs eventArgs)
    {
        try
        {
            RestartGameResult result = _applicationService.RestartGame(new RestartGameCommand());
            ApplyResult(
                result.IsSuccess,
                result.FailureMessage,
                result.Screen,
                result.RuntimeDirective);
        }
        catch (Exception exception)
        {
            _view.ShowFailure($"重新開始失敗：{exception.Message}");
        }
    }

    private void OnDirectionRequested(
        object? sender,
        DirectionRequestedEventArgs eventArgs)
    {
        try
        {
            SetSnakeDirectionResult result = _applicationService.SetSnakeDirection(
                new SetSnakeDirectionCommand(eventArgs.Direction));
            ApplyResult(
                result.IsSuccess,
                result.FailureMessage,
                result.Screen,
                result.RuntimeDirective);
        }
        catch (Exception exception)
        {
            _view.ShowFailure($"更新方向失敗：{exception.Message}");
        }
    }

    private void OnTickRequested(object? sender, GameTickEventArgs eventArgs)
    {
        try
        {
            ProcessGameTickResult result = _applicationService.ProcessGameTick(
                new ProcessGameTickCommand(eventArgs.AccumulatedElapsedMilliseconds));
            ApplyResult(
                result.IsSuccess,
                result.FailureMessage,
                result.Screen,
                result.RuntimeDirective);
        }
        catch (Exception exception)
        {
            _view.ShowFailure($"處理遊戲時間失敗：{exception.Message}");
        }
    }

    private void ApplyResult(
        bool isSuccess,
        string? failureMessage,
        GameScreenOutput? screen,
        RuntimeDirective runtimeDirective)
    {
        if (!isSuccess || screen is null)
        {
            _view.ShowFailure(failureMessage ?? "應用流程未回傳完整畫面。");
            return;
        }

        _view.ApplyRuntimeDirective(runtimeDirective);
        _view.RenderGameScreen(screen);
    }
}
