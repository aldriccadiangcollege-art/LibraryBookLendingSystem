using LibraryBookLendingSystem.Models.DTOs.Loans;
using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryBookLendingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Staff)]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loans;
    public LoansController(ILoanService loans) => _loans = loans;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool activeOnly = false) => Ok(await _loans.ListAsync(activeOnly));

    [HttpGet("member/{memberId:int}")]
    public async Task<IActionResult> History(int memberId) => Ok(await _loans.HistoryAsync(memberId));

    [HttpGet("unpaid-fines"), Authorize(Roles = Roles.Operators)]
    public async Task<IActionResult> UnpaidFines() => Ok(await _loans.UnpaidFinesAsync());

    [HttpPost, Authorize(Roles = Roles.Operators)]
    public async Task<IActionResult> Borrow(BorrowRequest request) =>
        StatusCode(StatusCodes.Status201Created, await _loans.BorrowAsync(request));

    [HttpPost("{id:int}/return"), Authorize(Roles = Roles.Operators)]
    public async Task<IActionResult> Return(int id, ReturnRequest request) => Ok(await _loans.ReturnAsync(id, request));

    [HttpPost("{id:int}/renew"), Authorize(Roles = Roles.Operators)]
    public async Task<IActionResult> Renew(int id) => Ok(await _loans.RenewAsync(id));

    [HttpPost("{id:int}/pay-fine"), Authorize(Roles = Roles.Operators)]
    public async Task<IActionResult> PayFine(int id) => Ok(await _loans.PayFineAsync(id));
}
