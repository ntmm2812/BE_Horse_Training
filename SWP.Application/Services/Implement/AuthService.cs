using SWP.Application.Common;
using SWP.Application.DTOs.Auth;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Repositories.Interface;
using SWP.Infrastructure.Security.Interface;

namespace SWP.Application.Services.Implement;

public class AuthService : IAuthService
{
    private const int MinPasswordLength = 6;

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

        if (string.Equals(user.Status, UserStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return LoginResult.Fail("Tài khoản đang chờ Quản lý CLB duyệt.", "PENDING_APPROVAL");
        }

        if (!string.Equals(user.Status, UserStatuses.Active, StringComparison.OrdinalIgnoreCase))
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

    public async Task<RegisterResult> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return RegisterResult.Fail("Họ và tên là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return RegisterResult.Fail("Email và mật khẩu là bắt buộc.");
        }

        if (request.Password.Length < MinPasswordLength)
        {
            return RegisterResult.Fail($"Mật khẩu phải có ít nhất {MinPasswordLength} ký tự.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return RegisterResult.Fail("Email này đã được đăng ký.", "EMAIL_EXISTS");
        }

        var roleId = await _userRepository.GetRoleIdByCanonicalRoleIdAsync(RoleNames.HorseOwner, cancellationToken);
        if (roleId is null)
        {
            roleId = await _userRepository.GetRoleIdByRoleNameAsync("Horse Owner", cancellationToken);
        }

        if (roleId is null)
        {
            return RegisterResult.Fail(
                "Không tìm thấy vai trò Horse Owner trong bảng Role. Kiểm tra RoleName = 'Horse Owner' trong database.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Status = UserStatuses.Pending,
            RoleId = roleId.Value,
            CreatedAt = DateTime.UtcNow,
        };

        await _userRepository.AddAsync(user, cancellationToken);

        return RegisterResult.Ok(new RegisterResponseDto
        {
            FullName = user.FullName,
            Email = user.Email,
            RoleId = RoleNames.HorseOwner,
            Status = user.Status,
        });
    }
}
