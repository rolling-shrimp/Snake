using Microsoft.Extensions.DependencyInjection;
using Snake.Application._GetGameScreenUseCase.Enums;
using Snake.Application;
using Snake.Application._EndCurrentLevelPlayUseCase;
using Snake.Application._EvaluateLevelCompletionUseCase;
using Snake.Application._GetGameScreenUseCase;
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
using Snake.Application._ReplenishTriggeredObjectUseCase.Port;
using Snake.Application._RescueWithShieldUseCase;
using Snake.Application._RestartGameUseCase;
using Snake.Application._SetSnakeDirectionUseCase;
using Snake.Application._StartFirstLevelUseCase;
using Snake.Application._StartSecondLevelUseCase;
using Snake.Application._StartThirdLevelUseCase;
using Snake.Application._UpdateThirdLevelDoorStateUseCase;
using Snake.Application.Repositories;
using Snake.Infrastructure;
using Snake.WinForms;

namespace Snake.Test
{
    public class NewArchitectureTests
    {
        private StaTestRunner _staTestRunner = new StaTestRunner();
        private GameFormTestActions _gameFormTestActions = new GameFormTestActions();

        [Fact]
        public void AddApplication_RegistersEveryConcreteUseCaseAsTransient()
        {
            ServiceCollection services = new ServiceCollection();

            services.AddApplication();

            Type[] useCaseTypes =
            {
                typeof(StartFirstLevelUseCase),
                typeof(StartSecondLevelUseCase),
                typeof(StartThirdLevelUseCase),
                typeof(SetSnakeDirectionUseCase),
                typeof(UpdateThirdLevelDoorStateUseCase),
                typeof(HandleOrdinaryFoodContactUseCase),
                typeof(HandleSpecialFoodContactUseCase),
                typeof(HandleShieldContactUseCase),
                typeof(HandleFireContactUseCase),
                typeof(HandleBombContactUseCase),
                typeof(HandleSelfCollisionUseCase),
                typeof(HandleWallCollisionUseCase),
                typeof(HandleClosedDoorCollisionUseCase),
                typeof(MoveSnakeUseCase),
                typeof(RescueWithShieldUseCase),
                typeof(EvaluateLevelCompletionUseCase),
                typeof(EndCurrentLevelPlayUseCase),
                typeof(PrepareNextLevelUseCase),
                typeof(ReplenishTriggeredObjectUseCase),
                typeof(RestartGameUseCase),
                typeof(GetGameScreenUseCase)
            };

            foreach (Type useCaseType in useCaseTypes)
            {
                ServiceDescriptor descriptor = Assert.Single(services.Where(item => item.ServiceType == useCaseType));
                Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
            }
        }

        [Fact]
        public void AddInfrastructure_RegistersOneSharedGameRepository()
        {
            ServiceCollection services = new ServiceCollection();

            services.AddInfrastructure();

            ServiceDescriptor descriptor = Assert.Single(
                services.Where(item => item.ServiceType == typeof(IGameRepository)));
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);

            using ServiceProvider provider = services.BuildServiceProvider();
            IGameRepository firstRepository = provider.GetRequiredService<IGameRepository>();
            IGameRepository secondRepository = provider.GetRequiredService<IGameRepository>();

            Assert.Same(firstRepository, secondRepository);
            Assert.Same(firstRepository.GetCurrentGame(), secondRepository.GetCurrentGame());
        }

        [Fact]
        public void GameFormTick_MovesSnakeExactlyOneCell()
        {
            _staTestRunner.Run(() =>
            {
                ServiceCollection services = new ServiceCollection();
                services.AddApplication();
                services.AddInfrastructure();
                services.AddSingleton<ISpawnPositionSelector, FirstLegalSpawnPositionSelector>();
                services.AddTransient<GameForm>();
                using ServiceProvider provider = services.BuildServiceProvider();
                using GameForm form = provider.GetRequiredService<GameForm>();
                GetGameScreenUseCase getGameScreenUseCase = provider.GetRequiredService<GetGameScreenUseCase>();

                GameScreenDTO readyScreen = getGameScreenUseCase.GetGameScreen();
                _gameFormTestActions.Start(form);
                GameScreenDTO startedScreen = getGameScreenUseCase.GetGameScreen();
                _gameFormTestActions.Tick(form);
                GameScreenDTO movedScreen = getGameScreenUseCase.GetGameScreen();

                Assert.Equal(GameScreenKind.FirstLevelReady, readyScreen.ScreenKind);
                Assert.Equal(GameScreenKind.FirstLevelPlaying, startedScreen.ScreenKind);
                Assert.Equal(startedScreen.SnakeCells[0].X + 1, movedScreen.SnakeCells[0].X);
                Assert.Equal(startedScreen.SnakeCells[0].Y, movedScreen.SnakeCells[0].Y);
                return true;
            });
        }
    }
}

