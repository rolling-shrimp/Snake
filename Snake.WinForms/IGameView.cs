namespace Snake.WinForms;

using Snake.UseCases;

public interface IGameView
{
    event EventHandler? StartRequested;
    event EventHandler? ReplayRequested;
    event EventHandler<DirectionRequestedEventArgs>? DirectionRequested;
    event EventHandler<GameTickEventArgs>? TickRequested;

    void ApplyRuntimeDirective(RuntimeDirective runtimeDirective);
    void RenderGameScreen(GameScreenOutput screen);
    void ShowFailure(string message);
}
