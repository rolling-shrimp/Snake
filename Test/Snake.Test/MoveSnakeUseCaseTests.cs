using Microsoft.Extensions.DependencyInjection;
using Snake.Application;
using Snake.Application.Repositories;
using Snake.Application._ReplenishTriggeredObjectUseCase.Port;
using Snake.Application._GetGameScreenUseCase;
using Snake.Application._MoveSnakeUseCase;
using Snake.Application._StartFirstLevelUseCase;
using Snake.Domain;
using Snake.Infrastructure;
using Snake.WinForms;

namespace Snake.Test
{
    public class MoveSnakeUseCaseTests
    {
        [Fact]
        public void MoveSnake_MovesExactlyOneCell()
        {
            using ServiceProvider provider = CreateProvider();
            StartFirstLevelUseCase startFirstLevelUseCase = provider.GetRequiredService<StartFirstLevelUseCase>();
            MoveSnakeUseCase moveSnakeUseCase = provider.GetRequiredService<MoveSnakeUseCase>();
            GetGameScreenUseCase getGameScreenUseCase = provider.GetRequiredService<GetGameScreenUseCase>();
            startFirstLevelUseCase.StartFirstLevel();
            int beforeX = getGameScreenUseCase.GetGameScreen().SnakeCells[0].X;

            moveSnakeUseCase.MoveSnake();

            Assert.Equal(beforeX + 1, getGameScreenUseCase.GetGameScreen().SnakeCells[0].X);
        }

        [Fact]
        public void MoveSnake_PreservesOrderedUniqueContinuousBody()
        {
            using ServiceProvider provider = CreateProvider();
            StartFirstLevelUseCase startFirstLevelUseCase = provider.GetRequiredService<StartFirstLevelUseCase>();
            MoveSnakeUseCase moveSnakeUseCase = provider.GetRequiredService<MoveSnakeUseCase>();
            Game game = provider.GetRequiredService<IGameRepository>().GetCurrentGame();
            startFirstLevelUseCase.StartFirstLevel();

            moveSnakeUseCase.MoveSnake();

            List<Snake.Domain.GridPosition> bodyCells = game.Snake.BodyCells;
            Assert.Equal(bodyCells.Count, bodyCells.Distinct().Count());
            Assert.True(game.Board.IsContinuous(bodyCells, game.Level.BoundaryPolicy));
        }

        private ServiceProvider CreateProvider()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddApplication();
            services.AddInfrastructure();
            services.AddSingleton<ISpawnPositionSelector, FirstLegalSpawnPositionSelector>();
            return services.BuildServiceProvider();
        }
    }
}

