using Microsoft.Extensions.DependencyInjection;
using SWP.Application.Services.Implement;
using SWP.Application.Services.Interface;

namespace SWP.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
