using SWP.Application.Common;
using SWP.Application.DTOs.Auth;

namespace SWP.Application.Services.Interface;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<RegisterResult> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
}
