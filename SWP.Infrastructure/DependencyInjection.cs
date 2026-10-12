using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SWP.Infrastructure.Options;
using SWP.Infrastructure.Repositories.Implement;
using SWP.Infrastructure.Services.Implement;
using SWP.Infrastructure.Services.Interface;
using SWP.Infrastructure.Repositories.Interface;
using SWP.Infrastructure.Security.Implement;
using SWP.Infrastructure.Security.Interface;

namespace SWP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<CloudinarySettings>(configuration.GetSection(CloudinarySettings.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHorseRepository, HorseRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IHorsePhotoStorage, CloudinaryHorsePhotoStorage>();

        return services;
    }
}
