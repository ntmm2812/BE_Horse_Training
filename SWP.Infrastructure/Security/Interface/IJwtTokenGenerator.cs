using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Security.Interface;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
