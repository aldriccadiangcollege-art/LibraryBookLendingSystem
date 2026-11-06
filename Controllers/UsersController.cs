using LibraryBookLendingSystem.Models.DTOs.Users;
using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryBookLendingSystem.Controllers;

/// <summary>Staff accounts. Only the administrator can manage them.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Administrator)]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    public UsersController(IUserService users) => _users = users;

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _users.ListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request) =>
        StatusCode(StatusCodes.Status201Created, await _users.CreateAsync(request));

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request)
    {
        await _users.ResetPasswordAsync(id, request);
        return NoContent();
    }

    [HttpPut("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive) => Ok(await _users.SetActiveAsync(id, isActive));
}
