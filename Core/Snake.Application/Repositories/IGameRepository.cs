using Snake.Domain;

namespace Snake.Application.Repositories
{
    public interface IGameRepository
    {
        Game GetCurrentGame();
    }
}
