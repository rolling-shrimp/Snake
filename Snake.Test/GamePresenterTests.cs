using Snake.Entities;
using Snake.UseCases;
using Snake.WinForms;

namespace Snake.Test;

public class GamePresenterTests
{
    [Fact]
    public void Initialize_RendersOneCompleteReadyScreen()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        SnakeGameApplicationService service = CreateService(session);
        GamePresenter presenter = new GamePresenter(view, service);

        presenter.Initialize();

        Assert.Equal(1, view.RenderCount);
        Assert.Equal(GameScreenKind.FirstLevelReady, view.CurrentScreen?.ScreenKind);
        Assert.Equal(RuntimeDirective.Unchanged, view.LastRuntimeDirective);
        Assert.Null(view.FailureMessage);
    }

    [Fact]
    public void StartEvent_CallsCompleteWorkflowAndRendersOnce()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, CreateService(session));
        presenter.Initialize();
        int renderCountBefore = view.RenderCount;

        view.RaiseStartRequested();

        Assert.Equal(renderCountBefore + 1, view.RenderCount);
        Assert.Equal(GameScreenKind.FirstLevelPlaying, view.CurrentScreen?.ScreenKind);
        Assert.Equal(RuntimeDirective.StartOrReset, view.LastRuntimeDirective);
        Assert.Equal(GamePhase.Playing, session.Load().Snapshot.Game.Phase);
    }

    [Fact]
    public void DirectionEvent_UpdatesDirectionWithoutMovingBody()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, CreateService(session));
        presenter.Initialize();
        view.RaiseStartRequested();
        List<GridPosition> bodyBefore = session.Load().Snapshot.Snake.BodyCells;

        view.RaiseDirectionRequested(Snake.UseCases.Direction.Up);

        Assert.Equal(bodyBefore, session.Load().Snapshot.Snake.BodyCells);
        Assert.Equal(Snake.Entities.Direction.Up, session.Load().Snapshot.Snake.QueuedDirection);
        Assert.Equal(GameScreenKind.FirstLevelPlaying, view.CurrentScreen?.ScreenKind);
    }

    [Fact]
    public void TickEvent_UsesAccumulatedTimeAndRendersFinalScreenOnce()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, CreateService(session));
        presenter.Initialize();
        view.RaiseStartRequested();
        int renderCountBefore = view.RenderCount;

        view.RaiseTickRequested(1000);

        Assert.Equal(renderCountBefore + 1, view.RenderCount);
        Assert.Equal(new GridPositionDTO(7, 1), view.CurrentScreen?.SnakeBody[0]);
        Assert.Equal(1000, session.Load().Snapshot.Game.LastProcessedElapsedMilliseconds);
    }

    [Fact]
    public void ReplayEvent_RestartsTerminalGameAndRendersOnePlayingScreen()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        VersionedSnakeGameSnapshot terminal = session.Load();
        Assert.True(terminal.Snapshot.Game.StartLevel());
        terminal.Snapshot.Game.EndGame();
        Assert.True(session.TryCommit(terminal.Version, terminal.Snapshot));
        GamePresenter presenter = new GamePresenter(view, CreateService(session));
        presenter.Initialize();
        int renderCountBefore = view.RenderCount;

        view.RaiseReplayRequested();

        Assert.Equal(renderCountBefore + 1, view.RenderCount);
        Assert.Equal(GameScreenKind.FirstLevelPlaying, view.CurrentScreen?.ScreenKind);
        Assert.Equal(RuntimeDirective.StartOrReset, view.LastRuntimeDirective);
    }

    private SnakeGameApplicationService CreateService(SnakeGameSession session)
    {
        return new SnakeGameApplicationService(
            session,
            new FirstLegalSpawnPositionSelector());
    }
}
