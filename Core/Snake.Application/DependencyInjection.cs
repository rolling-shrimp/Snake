using Microsoft.Extensions.DependencyInjection;
using Snake.Application._EndCurrentLevelPlayUseCase;
using Snake.Application._EvaluateLevelCompletionUseCase;
using Snake.Application.Common;
using Snake.Application._GetGameScreenUseCase;
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
using Snake.Application._RescueWithShieldUseCase;
using Snake.Application._RestartGameUseCase;
using Snake.Application._SetSnakeDirectionUseCase;
using Snake.Application._StartFirstLevelUseCase;
using Snake.Application._StartSecondLevelUseCase;
using Snake.Application._StartThirdLevelUseCase;
using Snake.Application._UpdateThirdLevelDoorStateUseCase;

namespace Snake.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddSingleton<SpawnPositionService>();
            services.AddTransient<StartFirstLevelUseCase>();
            services.AddTransient<StartSecondLevelUseCase>();
            services.AddTransient<StartThirdLevelUseCase>();
            services.AddTransient<SetSnakeDirectionUseCase>();
            services.AddTransient<UpdateThirdLevelDoorStateUseCase>();
            services.AddTransient<HandleSelfCollisionUseCase>();
            services.AddTransient<HandleWallCollisionUseCase>();
            services.AddTransient<HandleClosedDoorCollisionUseCase>();
            services.AddTransient<MoveSnakeUseCase>();
            services.AddTransient<HandleOrdinaryFoodContactUseCase>();
            services.AddTransient<HandleSpecialFoodContactUseCase>();
            services.AddTransient<HandleShieldContactUseCase>();
            services.AddTransient<HandleFireContactUseCase>();
            services.AddTransient<HandleBombContactUseCase>();
            services.AddTransient<RescueWithShieldUseCase>();
            services.AddTransient<EvaluateLevelCompletionUseCase>();
            services.AddTransient<EndCurrentLevelPlayUseCase>();
            services.AddTransient<PrepareNextLevelUseCase>();
            services.AddTransient<ReplenishTriggeredObjectUseCase>();
            services.AddTransient<RestartGameUseCase>();
            services.AddTransient<GetGameScreenUseCase>();
            return services;
        }
    }
}
