using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SWP.Infrastructure.Options;
using SWP.Infrastructure.Repositories.Implement;
using SWP.Infrastructure.Repositories.Interface;
using SWP.Infrastructure.Security.Implement;
using SWP.Infrastructure.Security.Interface;

namespace SWP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
