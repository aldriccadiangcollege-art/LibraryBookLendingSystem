using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryBookLendingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Reporters)]
public class AdminController : ControllerBase
{
    private readonly IBackupService _backup;
    public AdminController(IBackupService backup) => _backup = backup;

    /// <summary>Writes a full database backup (.bak) to the folder set in appsettings.json.</summary>
    [HttpPost("backup")]
    public async Task<IActionResult> Backup() => Ok(new { file = await _backup.BackupAsync() });
}
