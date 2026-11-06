using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryBookLendingSystem.Exceptions;
using LibraryBookLendingSystem.Models.DTOs.Auth;
using LibraryBookLendingSystem.Repositories.Interfaces;
using LibraryBookLendingSystem.Security;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace LibraryBookLendingSystem.Services.Implementations;

public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 10;

    private readonly IUserRepository _users;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository users, IAuditService audit, IConfiguration config)
    {
        _users = users;
        _audit = audit;
        _config = config;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var username = request.Username.Trim();
        var user = await _users.GetByUsernameAsync(username);

        if (user is null)
        {
            _audit.Record("LoginFailed", "Unknown username.", username);
            await _audit.SaveAsync();
            throw new UnauthorizedException("Invalid username or password.");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
            throw new UnauthorizedException($"Too many failed attempts. Try again after {user.LockoutEnd.Value:h:mm tt}.");

        if (!user.IsActive)
            throw new UnauthorizedException("This account is disabled. Ask the administrator.");

        if (!PasswordHelper.Verify(request.Password, user.PasswordHash))
        {
            user.FailedAttempts++;
            _audit.Record("LoginFailed", "Wrong password.", user.Username);
            if (user.FailedAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.Now.AddMinutes(LockoutMinutes);
                user.FailedAttempts = 0;
                _audit.Record("AccountLocked", $"Locked for {LockoutMinutes} minutes after repeated failures.", user.Username);
            }
            await _users.SaveAsync();
            throw new UnauthorizedException("Invalid username or password.");
        }

        user.FailedAttempts = 0;
        user.LockoutEnd = null;
        _audit.Record("Login", "Signed in.", user.Username);
        await _users.SaveAsync();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"] ?? "60"));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token),
            user.Username, user.FullName, user.Role, expires);
    }

    public async Task ChangePasswordAsync(string username, ChangePasswordRequest request)
    {
        var user = await _users.GetByUsernameAsync(username) ?? throw new NotFoundException("Account not found.");
        if (!PasswordHelper.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ConflictException("The current password is incorrect.");

        user.PasswordHash = PasswordHelper.Hash(request.NewPassword);
        _audit.Record("PasswordChanged", "User changed their own password.");
        await _users.SaveAsync();
    }
}
