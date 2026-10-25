using Snake.UseCases;
using Snake.WinForms;

namespace Snake.Test;

public class RecordingGameView : IGameView
{
    private GameViewModel? _currentViewModel;
    private string? _failureMessage;
    private int _renderCount;

    public event EventHandler? StartRequested;
    public event EventHandler? ReplayRequested;
    public event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;
    public event EventHandler<GameTickEventArgs>? TickRequested;

    public GameViewModel? CurrentViewModel => _currentViewModel;
    public string? FailureMessage => _failureMessage;
    public int RenderCount => _renderCount;

    public void Render(GameViewModel viewModel)
    {
        _currentViewModel = viewModel;
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

    public void RaiseTickRequested(int elapsedMilliseconds)
    {
        TickRequested?.Invoke(this, new GameTickEventArgs(elapsedMilliseconds));
    }
}
