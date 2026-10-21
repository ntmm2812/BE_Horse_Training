using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SWP.Application.Services.Implement;
using SWP.Application.Services.Interface;

namespace SWP.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountApprovalService, AccountApprovalService>();
        services.AddScoped<IHorseService, HorseService>();
        services.AddScoped<IHorseFitnessService, HorseFitnessService>();
        services.AddScoped<ITrainingPlanService, TrainingPlanService>();
        services.AddScoped<ITrainingSessionService, TrainingSessionService>();
        services.AddScoped<IMedicalService, MedicalService>();
        services.AddScoped<ICareService, CareService>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
