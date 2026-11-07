using Microsoft.Extensions.DependencyInjection;
using Snake.Application._GetGameScreenUseCase.Enums;
using Snake.Application;
using Snake.Application._EndCurrentLevelPlayUseCase;
using Snake.Application._EvaluateLevelCompletionUseCase;
using Snake.Application.Repositories;
using Snake.Application._GetGameScreenUseCase;
using Snake.Application._PrepareNextLevelUseCase;
using Snake.Application._ReplenishTriggeredObjectUseCase.Port;
using Snake.Infrastructure;
using Snake.Application._UpdateThirdLevelDoorStateUseCase;
using Snake.Domain;
using Snake.WinForms;

namespace Snake.Test
{
    public class GameProgressionAndHazardTests
    {
        private GameFormTestActions _gameFormTestActions = new GameFormTestActions();

        [Fact]
        public void OrdinaryFoodContact_GrowsOneCellAndReplenishesFood()
        {
            using ServiceProvider provider = CreateProvider();
            using GameForm form = provider.GetRequiredService<GameForm>();
            GetGameScreenUseCase getGameScreenUseCase = provider.GetRequiredService<GetGameScreenUseCase>();
            Game game = provider.GetRequiredService<IGameRepository>().GetCurrentGame();
            _gameFormTestActions.Start(form);
            game.Food.Reset();
            GridPosition nextHead = GetNextHead(game);
            game.Food.SpawnOrdinary(nextHead);

            _gameFormTestActions.Tick(form);

            Assert.Equal(4, getGameScreenUseCase.GetGameScreen().LevelStatus.CurrentLength);
            Assert.Single(getGameScreenUseCase.GetGameScreen().Objects);
            Assert.Equal(BoardObjectKind.OrdinaryFood, getGameScreenUseCase.GetGameScreen().Objects[0].Kind);
        }

        [Fact]
        public void PassedLevels_PrepareSecondAndThirdLevelReadyScreens()
        {
            using ServiceProvider provider = CreateProvider();
            using GameForm form = provider.GetRequiredService<GameForm>();
            GetGameScreenUseCase getGameScreenUseCase = provider.GetRequiredService<GetGameScreenUseCase>();
            _gameFormTestActions.Start(form);

            CompleteCurrentLevel(provider);

            Assert.Equal(GameScreenKind.SecondLevelReady, getGameScreenUseCase.GetGameScreen().ScreenKind);
            Assert.Equal(3, getGameScreenUseCase.GetGameScreen().LevelStatus.CurrentLength);
            Assert.Equal(0, getGameScreenUseCase.GetGameScreen().LevelStatus.RemainingShieldCharges);
            _gameFormTestActions.Start(form);
            Assert.Equal(4, getGameScreenUseCase.GetGameScreen().Objects.Count);

            CompleteCurrentLevel(provider);

            Assert.Equal(GameScreenKind.ThirdLevelReady, getGameScreenUseCase.GetGameScreen().ScreenKind);
            Assert.Equal(3, getGameScreenUseCase.GetGameScreen().LevelStatus.CurrentLength);
            Assert.NotEmpty(getGameScreenUseCase.GetGameScreen().WallCells);
            Assert.Single(getGameScreenUseCase.GetGameScreen().Doors);
        }

        [Fact]
        public void ThirdLevelDoor_UsesSixteenTickWarningAndTwentyFourTickToggle()
        {
            using ServiceProvider provider = CreateProvider();
            using GameForm form = EnterThirdLevel(provider);
            GetGameScreenUseCase getGameScreenUseCase = provider.GetRequiredService<GetGameScreenUseCase>();
            UpdateThirdLevelDoorStateUseCase updateDoor = provider.GetRequiredService<UpdateThirdLevelDoorStateUseCase>();

            for (int tick = 0; tick < 16; tick++)
            {
                updateDoor.UpdateThirdLevelDoorState();
            }

            Assert.Equal(DoorVisualState.ClosingWarning, getGameScreenUseCase.GetGameScreen().Doors[0].State);
            for (int tick = 16; tick < 24; tick++)
            {
                updateDoor.UpdateThirdLevelDoorState();
            }

            Assert.Equal(DoorVisualState.Closed, getGameScreenUseCase.GetGameScreen().Doors[0].State);
        }

        [Fact]
        public void BombWithoutShield_EndsTheGameWithoutReplenishingBomb()
        {
            using ServiceProvider provider = CreateProvider();
            using GameForm form = EnterThirdLevel(provider);
            GetGameScreenUseCase getGameScreenUseCase = provider.GetRequiredService<GetGameScreenUseCase>();
            Game game = provider.GetRequiredService<IGameRepository>().GetCurrentGame();
            ArrangeBombAtNextHead(game, false);

            _gameFormTestActions.Tick(form);

            Assert.Equal(GameScreenKind.GameOver, getGameScreenUseCase.GetGameScreen().ScreenKind);
            Assert.Empty(getGameScreenUseCase.GetGameScreen().Objects);
            Assert.Equal(DeathCause.Bomb.ToString(), getGameScreenUseCase.GetGameScreen().Result!.DeathCause);
        }

        [Fact]
        public void BombWithShield_ConsumesOneChargeAndContinuesPlaying()
        {
            using ServiceProvider provider = CreateProvider();
            using GameForm form = EnterThirdLevel(provider);
            GetGameScreenUseCase getGameScreenUseCase = provider.GetRequiredService<GetGameScreenUseCase>();
            Game game = provider.GetRequiredService<IGameRepository>().GetCurrentGame();
            ArrangeBombAtNextHead(game, true);

            _gameFormTestActions.Tick(form);

            Assert.Equal(GameScreenKind.ThirdLevelPlaying, getGameScreenUseCase.GetGameScreen().ScreenKind);
            Assert.Equal(0, getGameScreenUseCase.GetGameScreen().LevelStatus.RemainingShieldCharges);
            Assert.Contains(getGameScreenUseCase.GetGameScreen().Objects, gameObject => gameObject.Kind == BoardObjectKind.Bomb);
        }

        private GameForm EnterThirdLevel(ServiceProvider provider)
        {
            GameForm form = provider.GetRequiredService<GameForm>();
            GetGameScreenUseCase getGameScreenUseCase = provider.GetRequiredService<GetGameScreenUseCase>();
            _gameFormTestActions.Start(form);
            CompleteCurrentLevel(provider);
            _gameFormTestActions.Start(form);
            CompleteCurrentLevel(provider);
            _gameFormTestActions.Start(form);
            Assert.Equal(GameScreenKind.ThirdLevelPlaying, getGameScreenUseCase.GetGameScreen().ScreenKind);
            return form;
        }

        private void CompleteCurrentLevel(ServiceProvider provider)
        {
            Game game = provider.GetRequiredService<IGameRepository>().GetCurrentGame();
            List<GridPosition>? completedBody = game.Board.FindSafeBody(
                game.Level.TargetLength,
                game.Level.BoundaryPolicy,
                game.GetMovementBlockedCells());
            Assert.NotNull(completedBody);
            game.Snake.ResetSafely(completedBody!, game.Board, game.Level.BoundaryPolicy);
            provider.GetRequiredService<EvaluateLevelCompletionUseCase>().EvaluateLevelCompletion();
            provider.GetRequiredService<EndCurrentLevelPlayUseCase>().EndCurrentLevelPlay();
            provider.GetRequiredService<PrepareNextLevelUseCase>().PrepareNextLevel();
        }

        private void ArrangeBombAtNextHead(Game game, bool addShieldCharge)
        {
            game.ResetObjects();
            if (addShieldCharge)
            {
                game.Shield.Spawn(new GridPosition(1, 1));
                game.Shield.Collect();
            }

            game.Bomb.Spawn(GetNextHead(game));
        }

        private GridPosition GetNextHead(Game game)
        {
            BoardMovementResult movement = game.Board.ResolveMovement(
                game.Snake.BodyCells[0],
                game.Snake.NextDirection,
                game.Level.BoundaryPolicy);
            return Assert.IsType<GridPosition>(movement.Position);
        }

        private ServiceProvider CreateProvider()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddApplication();
            services.AddInfrastructure();
            services.AddSingleton<ISpawnPositionSelector, FirstLegalSpawnPositionSelector>();
            services.AddTransient<GameForm>();
            return services.BuildServiceProvider();
        }
    }
}


