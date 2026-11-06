using LibraryBookLendingSystem.Exceptions;
using LibraryBookLendingSystem.Models.DTOs.Users;
using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Repositories.Interfaces;
using LibraryBookLendingSystem.Security;
using LibraryBookLendingSystem.Services.Interfaces;

namespace LibraryBookLendingSystem.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IAuditService _audit;
    private readonly IHttpContextAccessor _http;

    public UserService(IUserRepository users, IAuditService audit, IHttpContextAccessor http)
    {
        _users = users;
        _audit = audit;
        _http = http;
    }

    private static UserResponse ToResponse(User u) => new(u.Id, u.Username, u.FullName, u.Role, u.IsActive);

    public async Task<List<UserResponse>> ListAsync() => (await _users.ListAsync()).Select(ToResponse).ToList();

    public async Task<UserResponse> CreateAsync(CreateUserRequest r)
    {
        var username = r.Username.Trim();
        if (await _users.UsernameExistsAsync(username))
            throw new ConflictException($"The username '{username}' is already taken.");

        var user = new User
        {
            Username = username,
            FullName = r.FullName.Trim(),
            Role = r.Role,
            PasswordHash = PasswordHelper.Hash(r.Password)
        };
        _users.Add(user);
        _audit.Record("UserCreated", $"Account '{username}' created with role {r.Role}.");
        await _users.SaveAsync();
        return ToResponse(user);
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordRequest r)
    {
        var user = await _users.GetAsync(id) ?? throw new NotFoundException($"User {id} was not found.");
        user.PasswordHash = PasswordHelper.Hash(r.NewPassword);
        user.FailedAttempts = 0;
        user.LockoutEnd = null;
        _audit.Record("PasswordReset", $"Password reset for '{user.Username}'.");
        await _users.SaveAsync();
    }

    public async Task<UserResponse> SetActiveAsync(int id, bool isActive)
    {
        var user = await _users.GetAsync(id) ?? throw new NotFoundException($"User {id} was not found.");
        var me = _http.HttpContext?.User.Identity?.Name;
        if (!isActive && string.Equals(me, user.Username, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("You cannot disable your own account.");

        user.IsActive = isActive;
        _audit.Record(isActive ? "UserEnabled" : "UserDisabled", $"Account '{user.Username}'.");
        await _users.SaveAsync();
        return ToResponse(user);
    }
}
