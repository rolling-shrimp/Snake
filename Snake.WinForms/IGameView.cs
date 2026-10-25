namespace Snake.WinForms;

public interface IGameView
{
    event EventHandler? StartRequested;
    event EventHandler? ReplayRequested;
    event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;
    event EventHandler<GameTickEventArgs>? TickRequested;

    void Render(GameViewModel viewModel);
    void ShowFailure(string message);
}
