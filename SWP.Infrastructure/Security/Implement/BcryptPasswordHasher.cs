using SWP.Infrastructure.Security.Interface;

namespace SWP.Infrastructure.Security.Implement;

public class BcryptPasswordHasher : IPasswordHasher
{
    public bool Verify(string password, string? passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
}
