using Snake.UseCases;
using Snake.WinForms;

namespace Snake.Test;

public class RecordingGameView : IGameView
{
    private GameScreenOutput? _currentScreen;
    private string? _failureMessage;
    private RuntimeDirective _lastRuntimeDirective;
    private int _renderCount;

    public event EventHandler? StartRequested;
    public event EventHandler? ReplayRequested;
    public event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;
    public event EventHandler<GameTickEventArgs>? TickRequested;

    public GameScreenOutput? CurrentScreen => _currentScreen;
    public string? FailureMessage => _failureMessage;
    public RuntimeDirective LastRuntimeDirective => _lastRuntimeDirective;
    public int RenderCount => _renderCount;

    public void ApplyRuntimeDirective(RuntimeDirective runtimeDirective)
    {
        _lastRuntimeDirective = runtimeDirective;
    }

    public void RenderGameScreen(GameScreenOutput screen)
    {
        _currentScreen = screen;
        _renderCount++;
    }

    public void ShowFailure(string message)
    {
        _failureMessage = message;
    }

    public void RaiseStartRequested()
    {
        StartRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RaiseReplayRequested()
    {
        ReplayRequested?.Invoke(this, EventArgs.Empty);
    }

    public void RaiseDirectionRequested(Direction direction)
    {
        DirectionRequested?.Invoke(this, new DirectionRequestedEventArgs(direction));
    }

    public void RaiseTickRequested(long accumulatedElapsedMilliseconds)
    {
        TickRequested?.Invoke(
            this,
            new GameTickEventArgs(accumulatedElapsedMilliseconds));
    }
}
