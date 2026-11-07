using Microsoft.Extensions.DependencyInjection;
using Snake.Application;
using Snake.Application._ReplenishTriggeredObjectUseCase.Port;
using Snake.Infrastructure;

namespace Snake.WinForms
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            ServiceCollection services = new ServiceCollection();
            services.AddApplication();
            services.AddInfrastructure();
            services.AddSingleton<RandomSpawnPositionSelector>();
            services.AddSingleton<ISpawnPositionSelector>(serviceProvider =>
                serviceProvider.GetRequiredService<RandomSpawnPositionSelector>());
            services.AddTransient<GameForm>();

            using ServiceProvider serviceProvider = services.BuildServiceProvider();
            System.Windows.Forms.Application.Run(serviceProvider.GetRequiredService<GameForm>());
        }
    }
}
