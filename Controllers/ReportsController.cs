using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryBookLendingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    public ReportsController(IReportService reports) => _reports = reports;

    // ----- for all staff -----
    [HttpGet("dashboard"), Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> Dashboard() => Ok(await _reports.DashboardAsync());

    [HttpGet("shelf-list"), Authorize(Roles = Roles.Staff)]
    public async Task<IActionResult> ShelfList() => Ok(await _reports.ShelfListAsync());

    // ----- Librarian and LR Coordinator: daily circulation work -----
    [HttpGet("reminders"), Authorize(Roles = Roles.Operators)]
    public async Task<IActionResult> Reminders() => Ok(await _reports.RemindersAsync());

    // ----- Librarian and Administrator: monitoring reports -----
    [HttpGet("borrowers-log"), Authorize(Roles = Roles.Reporters)]
    public async Task<IActionResult> BorrowersLog([FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        Ok(await _reports.BorrowersLogAsync(from, to));

    [HttpGet("usage"), Authorize(Roles = Roles.Reporters)]
    public async Task<IActionResult> Usage([FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        Ok(await _reports.UsageAsync(from, to));

    [HttpGet("overdue"), Authorize(Roles = Roles.Reporters)]
    public async Task<IActionResult> Overdue() => Ok(await _reports.OverdueAsync());

    [HttpGet("fines"), Authorize(Roles = Roles.Reporters)]
    public async Task<IActionResult> Fines() => Ok(await _reports.FinesAsync());

    [HttpGet("collection-review"), Authorize(Roles = Roles.Reporters)]
    public async Task<IActionResult> CollectionReview() => Ok(await _reports.CollectionReviewAsync());

    [HttpGet("duplicates"), Authorize(Roles = Roles.Reporters)]
    public async Task<IActionResult> Duplicates() => Ok(await _reports.DuplicatesAsync());

    [HttpGet("withdrawn"), Authorize(Roles = Roles.Reporters)]
    public async Task<IActionResult> Withdrawn() => Ok(await _reports.WithdrawnAsync());

    [HttpGet("audit-log"), Authorize(Roles = Roles.Reporters)]
    public async Task<IActionResult> AuditLog() => Ok(await _reports.AuditLogAsync());
}
