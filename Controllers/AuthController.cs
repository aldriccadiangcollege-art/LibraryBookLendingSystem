using LibraryBookLendingSystem.Models.DTOs.Auth;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryBookLendingSystem.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request) => Ok(await _auth.LoginAsync(request));

    [HttpPost("change-password"), Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        await _auth.ChangePasswordAsync(User.Identity!.Name!, request);
        return NoContent();
    }
}
