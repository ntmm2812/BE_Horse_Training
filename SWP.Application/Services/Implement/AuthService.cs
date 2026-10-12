using SWP.Application.Common;
using SWP.Application.DTOs.Auth;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Infrastructure.Repositories.Interface;
using SWP.Infrastructure.Security.Interface;

namespace SWP.Application.Services.Implement;

public class AuthService : IAuthService
{
    private const string ActiveStatus = "Active";
    private const string PendingStatus = "Pending";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResult> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return LoginResult.Fail("Email và mật khẩu là bắt buộc.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailWithRoleAsync(normalizedEmail, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return LoginResult.Fail("Email hoặc mật khẩu không đúng.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return LoginResult.Fail("Email hoặc mật khẩu không đúng.");
        }

        if (string.Equals(user.Status, PendingStatus, StringComparison.OrdinalIgnoreCase))
        {
            return LoginResult.Fail("Tài khoản đang chờ Quản lý CLB duyệt.", "PENDING_APPROVAL");
        }

        if (!string.Equals(user.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            return LoginResult.Fail("Tài khoản không được phép đăng nhập.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);
        var response = new LoginResponseDto
        {
            Token = token,
            User = new AuthUserDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                RoleId = RoleMapper.ToRoleId(user.Role.RoleName),
                Status = user.Status,
            },
        };

        return LoginResult.Ok(response);
    }
}
