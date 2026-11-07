using Microsoft.Extensions.DependencyInjection;
using Snake.Application._GetGameScreenUseCase.Enums;
using Snake.Application;
using Snake.Application._GetGameScreenUseCase;
using Snake.Application._ReplenishTriggeredObjectUseCase.Port;
using Snake.Application._SetSnakeDirectionUseCase;
using Snake.Application._SetSnakeDirectionUseCase.Dto;
using Snake.Infrastructure;
using Snake.WinForms;

namespace Snake.Test
{
    public class GameFormBehaviorTests
    {
        private StaTestRunner _staTestRunner = new StaTestRunner();
        private GameFormTestActions _gameFormTestActions = new GameFormTestActions();

        [Fact]
        public void SetSnakeDirection_IsUsedByTheNextSingleTick()
        {
            _staTestRunner.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                using GameForm form = provider.GetRequiredService<GameForm>();
                GetGameScreenUseCase getGameScreenUseCase = provider.GetRequiredService<GetGameScreenUseCase>();
                _gameFormTestActions.Start(form);
                int beforeX = getGameScreenUseCase.GetGameScreen().SnakeCells[0].X;
                int beforeY = getGameScreenUseCase.GetGameScreen().SnakeCells[0].Y;

                provider.GetRequiredService<SetSnakeDirectionUseCase>()
                    .SetSnakeDirection(new SnakeDirectionDTO(0, 1));
                _gameFormTestActions.Tick(form);

                Assert.Equal(beforeX, getGameScreenUseCase.GetGameScreen().SnakeCells[0].X);
                Assert.Equal(beforeY + 1, getGameScreenUseCase.GetGameScreen().SnakeCells[0].Y);
                return true;
            });
        }

        [Fact]
        public void StartFirstLevel_SpawnsOnlyOrdinaryFood()
        {
            _staTestRunner.Run(() =>
            {
                using ServiceProvider provider = CreateProvider();
                using GameForm form = provider.GetRequiredService<GameForm>();
                GetGameScreenUseCase getGameScreenUseCase = provider.GetRequiredService<GetGameScreenUseCase>();

                _gameFormTestActions.Start(form);

                Assert.Equal(GameScreenKind.FirstLevelPlaying, getGameScreenUseCase.GetGameScreen().ScreenKind);
                Assert.Single(getGameScreenUseCase.GetGameScreen().Objects);
                Assert.Equal(BoardObjectKind.OrdinaryFood, getGameScreenUseCase.GetGameScreen().Objects[0].Kind);
                return true;
            });
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

