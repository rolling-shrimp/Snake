using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Infrastructure.Repositories._Game
{
    public class GameRepository : IGameRepository
    {
        private Game _currentGame;

        public GameRepository()
        {
            _currentGame = new Game();
        }

        public Game GetCurrentGame()
        {
            return _currentGame;
        }
    }
}
