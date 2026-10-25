using Snake.UseCases;
using Snake.WinForms;
using Snake.Entities;

namespace Snake.Test;

public class GamePresenterTests
{
    [Fact]
    public void Initialize_RendersOnlyTheRequiredReadyInformation()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);

        presenter.Initialize();

        Assert.NotNull(view.CurrentViewModel);
        Assert.Equal("first-level-ready", view.CurrentViewModel.ScreenId);
        Assert.True(view.CurrentViewModel.Board.IsVisible);
        Assert.Equal(10, view.CurrentViewModel.Board.Rows);
        Assert.Equal(10, view.CurrentViewModel.Board.Columns);
        Assert.Equal(3, view.CurrentViewModel.Board.SnakeCells.Count);
        Assert.Equal(3, view.CurrentViewModel.RuleHints.Count);
        Assert.NotNull(view.CurrentViewModel.Status);
        Assert.Equal("第一關", view.CurrentViewModel.Status.LevelName);
        Assert.Equal(3, view.CurrentViewModel.Status.CurrentLength);
        Assert.Equal(20, view.CurrentViewModel.Status.TargetLength);
        Assert.Equal(15, view.CurrentViewModel.Status.ProgressPercent);
        Assert.True(view.CurrentViewModel.IsStartButtonVisible);
        Assert.Empty(view.CurrentViewModel.Board.Objects);
        Assert.False(view.CurrentViewModel.IsMovementActive);
        Assert.False(view.CurrentViewModel.IsReplayButtonVisible);
        Assert.Null(view.CurrentViewModel.ResultCard);
        Assert.Null(view.FailureMessage);
    }

    [Fact]
    public void StartRequested_RendersCompletePlayingStateAfterReactionCompletes()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        presenter.Initialize();

        view.RaiseStartRequested();

        Assert.NotNull(view.CurrentViewModel);
        Assert.Equal("first-level-playing", view.CurrentViewModel.ScreenId);
        Assert.False(view.CurrentViewModel.IsStartButtonVisible);
        Assert.Single(
            view.CurrentViewModel.Board.Objects,
            gameObject => gameObject.Type == GameObjectType.Strawberry);
        Assert.True(view.CurrentViewModel.IsMovementActive);
        Assert.Equal(250, view.CurrentViewModel.MovementIntervalMilliseconds);
        Assert.Equal(2, view.RenderCount);
        Assert.Null(view.FailureMessage);
    }

    [Fact]
    public void TickRequested_MovesSnakeOneCellAndRendersUpdatedBoard()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        presenter.Initialize();
        view.RaiseStartRequested();

        view.RaiseTickRequested(250);

        Assert.NotNull(view.CurrentViewModel);
        Assert.Equal("first-level-playing", view.CurrentViewModel.ScreenId);
        Assert.Equal(4, view.CurrentViewModel.Board.SnakeCells[0].X);
        Assert.Equal(1, view.CurrentViewModel.Board.SnakeCells[0].Y);
        Assert.Equal(3, view.RenderCount);
        Assert.Null(view.FailureMessage);
    }

    [Fact]
    public void LevelCompletion_RendersNextReadyScreensAndAllAdvancedObjects()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        presenter.Initialize();
        view.RaiseStartRequested();
        PrepareOrdinaryFoodCompletion(session, 19);

        view.RaiseTickRequested(250);

        Assert.NotNull(view.CurrentViewModel);
        Assert.Equal("second-level-ready", view.CurrentViewModel.ScreenId);
        Assert.True(view.CurrentViewModel.IsStartButtonVisible);
        Assert.NotNull(view.CurrentViewModel.ResultCard);
        Assert.Equal(1, view.CurrentViewModel.ResultCard.LevelNumber);
        Assert.Equal(20, view.CurrentViewModel.ResultCard.FinalLength);
        Assert.Equal(0, view.CurrentViewModel.Status?.ShieldRemainingCharges);

        view.RaiseStartRequested();

        Assert.Equal("second-level-playing", view.CurrentViewModel.ScreenId);
        Assert.Equal(
            new[]
            {
                GameObjectType.Strawberry,
                GameObjectType.SpecialFood,
                GameObjectType.Shield,
                GameObjectType.Fire
            },
            view.CurrentViewModel.Board.Objects.Select(gameObject => gameObject.Type));
        PrepareOrdinaryFoodCompletion(session, 19);

        view.RaiseTickRequested(250);

        Assert.Equal("third-level-ready", view.CurrentViewModel.ScreenId);
        Assert.True(view.CurrentViewModel.IsStartButtonVisible);
        Assert.NotNull(view.CurrentViewModel.ResultCard);
        Assert.Equal(2, view.CurrentViewModel.ResultCard.LevelNumber);
        Assert.NotEmpty(view.CurrentViewModel.Board.WallCells);
        Assert.NotEmpty(view.CurrentViewModel.Board.Doors);

        view.RaiseStartRequested();

        Assert.Equal("third-level-playing", view.CurrentViewModel.ScreenId);
        Assert.Equal(
            new[]
            {
                GameObjectType.Strawberry,
                GameObjectType.SpecialFood,
                GameObjectType.Shield,
                GameObjectType.Fire,
                GameObjectType.Bomb
            },
            view.CurrentViewModel.Board.Objects.Select(gameObject => gameObject.Type));
        Assert.True(view.CurrentViewModel.IsMovementActive);
        Assert.Null(view.FailureMessage);
    }

    [Fact]
    public void ShieldRescue_PreservesLengthAndContinuesSecondLevelPlaying()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        AdvanceToSecondLevelPlaying(view, presenter, session);
        session.Snake.Reset(CreateBodyWithRightOpen(session, 5));
        session.Shield.Reset();
        session.Shield.Spawn(
            new ShieldItemId(Guid.NewGuid()),
            new GridPosition(5, 5));

        view.RaiseTickRequested(250);

        Assert.Equal(1, view.CurrentViewModel?.Status?.ShieldRemainingCharges);
        Assert.Equal(5, view.CurrentViewModel?.Status?.CurrentLength);
        session.Snake.Reset(new List<GridPosition>
        {
            new GridPosition(2, 2),
            new GridPosition(3, 2),
            new GridPosition(3, 3),
            new GridPosition(2, 3),
            new GridPosition(1, 3)
        });

        view.RaiseTickRequested(250);

        Assert.Equal("second-level-playing", view.CurrentViewModel?.ScreenId);
        Assert.Equal(5, view.CurrentViewModel?.Status?.CurrentLength);
        Assert.Equal(0, view.CurrentViewModel?.Status?.ShieldRemainingCharges);
        Assert.True(view.CurrentViewModel?.IsMovementActive);
        Assert.Null(view.CurrentViewModel?.ResultCard);
    }

    [Fact]
    public void FireContact_ReducesLengthAndReplenishesFire()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        AdvanceToSecondLevelPlaying(view, presenter, session);
        session.Snake.Reset(CreateBodyWithRightOpen(session, 5));
        session.Fire.Reset();
        session.Fire.Spawn(
            new FireItemId(Guid.NewGuid()),
            new GridPosition(5, 5));

        view.RaiseTickRequested(250);

        Assert.Equal("second-level-playing", view.CurrentViewModel?.ScreenId);
        Assert.Equal(3, view.CurrentViewModel?.Status?.CurrentLength);
        Assert.Single(
            view.CurrentViewModel?.Board.Objects ?? new List<BoardObjectViewModel>(),
            gameObject => gameObject.Type == GameObjectType.Fire);
    }

    [Fact]
    public void SpecialFoodContact_GrowsByTwoAndReplenishesSpecialFood()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        AdvanceToSecondLevelPlaying(view, presenter, session);
        session.Snake.Reset(CreateBodyWithRightOpen(session, 5));
        session.Food.Reset();
        session.Food.SpawnSpecial(
            new FoodItemId(Guid.NewGuid()),
            new GridPosition(5, 5));

        view.RaiseTickRequested(250);

        Assert.Equal("second-level-playing", view.CurrentViewModel?.ScreenId);
        Assert.Equal(7, view.CurrentViewModel?.Status?.CurrentLength);
        Assert.Single(
            view.CurrentViewModel?.Board.Objects ?? new List<BoardObjectViewModel>(),
            gameObject => gameObject.Type == GameObjectType.SpecialFood);
    }

    [Fact]
    public void FatalFireWithoutShield_ShowsZeroProgressGameOver()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        AdvanceToSecondLevelPlaying(view, presenter, session);
        session.Snake.Reset(CreateBodyWithRightOpen(session, 2));
        session.Shield.Reset();
        session.Fire.Reset();
        session.Fire.Spawn(
            new FireItemId(Guid.NewGuid()),
            new GridPosition(5, 5));

        view.RaiseTickRequested(250);

        Assert.Equal("game-over", view.CurrentViewModel?.ScreenId);
        Assert.Equal(0, view.CurrentViewModel?.Status?.CurrentLength);
        Assert.Equal(0, view.CurrentViewModel?.Status?.ProgressPercent);
        Assert.Contains("火焰", view.CurrentViewModel?.ResultCard?.Detail);
    }

    [Fact]
    public void BombWithShield_RescuesAndReplenishesBombInThirdLevel()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        AdvanceToThirdLevelPlaying(view, presenter, session);
        session.Snake.Reset(CreateBodyWithRightOpen(session, 5, 6, 5, 7, 5));
        session.Shield.Reset();
        ShieldItemId shieldItemId = new ShieldItemId(Guid.NewGuid());
        session.Shield.Spawn(shieldItemId, new GridPosition(8, 8));
        session.Shield.Collect(shieldItemId);
        session.Bomb.Reset();
        session.Bomb.Spawn(
            new BombItemId(Guid.NewGuid()),
            new GridPosition(7, 5));

        view.RaiseTickRequested(250);

        Assert.Equal("third-level-playing", view.CurrentViewModel?.ScreenId);
        Assert.Equal(5, view.CurrentViewModel?.Status?.CurrentLength);
        Assert.Equal(0, view.CurrentViewModel?.Status?.ShieldRemainingCharges);
        Assert.Single(
            view.CurrentViewModel?.Board.Objects ?? new List<BoardObjectViewModel>(),
            gameObject => gameObject.Type == GameObjectType.Bomb);
    }

    [Fact]
    public void ThirdLevelDoorCycle_RendersWarningThenClosedState()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        AdvanceToThirdLevelPlaying(view, presenter, session);

        view.RaiseTickRequested(4000);

        Assert.Single(view.CurrentViewModel?.Board.Doors ?? new List<DoorViewModel>());
        Assert.Equal("ClosingWarning", view.CurrentViewModel?.Board.Doors[0].State);

        view.RaiseTickRequested(2000);

        Assert.Equal("Closed", view.CurrentViewModel?.Board.Doors[0].State);
    }

    [Fact]
    public void FatalCollision_ShowsGameOverAndReplayStartsFirstLevelImmediately()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        presenter.Initialize();
        view.RaiseStartRequested();
        session.Snake.Reset(new List<GridPosition>
        {
            new GridPosition(2, 2),
            new GridPosition(3, 2),
            new GridPosition(3, 3)
        });

        view.RaiseTickRequested(250);

        Assert.Equal("game-over", view.CurrentViewModel?.ScreenId);
        Assert.False(view.CurrentViewModel?.Board.IsVisible);
        Assert.True(view.CurrentViewModel?.IsReplayButtonVisible);
        Assert.False(view.CurrentViewModel?.IsMovementActive);
        Assert.Equal("Game Over", view.CurrentViewModel?.ResultCard?.Title);
        Assert.Single(session.PublishedFacts.OfType<SelfCollisionDomainEvent>());

        view.RaiseReplayRequested();

        Assert.Equal("first-level-playing", view.CurrentViewModel?.ScreenId);
        Assert.True(view.CurrentViewModel?.Board.IsVisible);
        Assert.True(view.CurrentViewModel?.IsMovementActive);
        Assert.False(view.CurrentViewModel?.IsReplayButtonVisible);
        Assert.Single(
            view.CurrentViewModel?.Board.Objects ?? new List<BoardObjectViewModel>(),
            gameObject => gameObject.Type == GameObjectType.Strawberry);
    }

    [Fact]
    public void ThirdLevelCompletion_ShowsFrozenBoardAndReplayStartsFirstLevel()
    {
        RecordingGameView view = new RecordingGameView();
        SnakeGameSession session = new SnakeGameSession();
        GamePresenter presenter = new GamePresenter(view, session);
        AdvanceToThirdLevelPlaying(view, presenter, session);
        PrepareOrdinaryFoodCompletion(session, 49);

        view.RaiseTickRequested(250);

        Assert.Equal("game-completed", view.CurrentViewModel?.ScreenId);
        Assert.True(view.CurrentViewModel?.Board.IsVisible);
        Assert.False(view.CurrentViewModel?.IsMovementActive);
        Assert.True(view.CurrentViewModel?.IsReplayButtonVisible);
        Assert.Equal(50, view.CurrentViewModel?.ResultCard?.FinalLength);
        Assert.Equal("闖關成功", view.CurrentViewModel?.ResultCard?.Title);

        view.RaiseReplayRequested();

        Assert.Equal("first-level-playing", view.CurrentViewModel?.ScreenId);
        Assert.True(view.CurrentViewModel?.IsMovementActive);
        Assert.Null(view.CurrentViewModel?.ResultCard);
    }

    private void AdvanceToSecondLevelPlaying(
        RecordingGameView view,
        GamePresenter presenter,
        SnakeGameSession session)
    {
        presenter.Initialize();
        view.RaiseStartRequested();
        PrepareOrdinaryFoodCompletion(session, 19);
        view.RaiseTickRequested(250);
        view.RaiseStartRequested();
    }

    private void AdvanceToThirdLevelPlaying(
        RecordingGameView view,
        GamePresenter presenter,
        SnakeGameSession session)
    {
        AdvanceToSecondLevelPlaying(view, presenter, session);
        PrepareOrdinaryFoodCompletion(session, 19);
        view.RaiseTickRequested(250);
        view.RaiseStartRequested();
    }

    private List<GridPosition> CreateBodyWithRightOpen(
        SnakeGameSession session,
        int length,
        int headX = 4,
        int headY = 5,
        int nextX = 5,
        int nextY = 5)
    {
        GridPosition head = new GridPosition(headX, headY);
        GridPosition next = new GridPosition(nextX, nextY);
        List<GridPosition> body = new List<GridPosition>
        {
            head
        };
        List<GridPosition> legalCells = session.Board.GetLegalSpawnCells(
            new List<GridPosition>(),
            session.GetObjectOccupiedCells());
        foreach (GridPosition position in legalCells)
        {
            if (!position.Equals(head) && !position.Equals(next))
            {
                body.Add(position);
            }

            if (body.Count == length)
            {
                break;
            }
        }

        Assert.Equal(length, body.Count);
        return body;
    }

    private void PrepareOrdinaryFoodCompletion(SnakeGameSession session, int currentLength)
    {
        bool isThirdLevel = session.Level.Number == LevelNumber.Third;
        int headX = isThirdLevel ? 6 : 4;
        int nextX = isThirdLevel ? 7 : 5;
        session.Snake.Reset(CreateBodyWithRightOpen(
            session,
            currentLength,
            headX,
            5,
            nextX,
            5));
        session.Food.Reset();
        session.Food.SpawnOrdinary(
            new FoodItemId(Guid.NewGuid()),
            new GridPosition(nextX, 5));
    }
}
