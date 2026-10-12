namespace SWP.Infrastructure.Security.Interface;

public interface IPasswordHasher
{
    bool Verify(string password, string? passwordHash);

    string Hash(string password);
}
