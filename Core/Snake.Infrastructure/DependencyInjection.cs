using Microsoft.Extensions.DependencyInjection;
using Snake.Application.Repositories;
using Snake.Infrastructure.Repositories._Game;

namespace Snake.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IGameRepository, GameRepository>();
            return services;
        }
    }
}
