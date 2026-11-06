using System.Text;
using LibraryBookLendingSystem.Models.DTOs.Books;
using LibraryBookLendingSystem.Models.DTOs.Common;
using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryBookLendingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Staff)]
public class BooksController : ControllerBase
{
    private readonly IBookService _books;
    public BooksController(IBookService books) => _books = books;

    // Students can search the catalog without logging in. Withdrawn books are shown to logged-in staff only.
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] string? category,
        [FromQuery] bool availableOnly = false, [FromQuery] bool includeWithdrawn = false) =>
        Ok(await _books.GetAllAsync(q, category, availableOnly, includeWithdrawn && User.Identity?.IsAuthenticated == true));

    [HttpGet("categories"), AllowAnonymous]
    public async Task<IActionResult> Categories() => Ok(await _books.CategoriesAsync());

    [HttpGet("{id:int}"), AllowAnonymous]
    public async Task<IActionResult> Get(int id) => Ok(await _books.GetAsync(id));

    [HttpPost, Authorize(Roles = Roles.Operators)]
    public async Task<IActionResult> Create(BookRequest request)
    {
        var book = await _books.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = book.Id }, book);
    }

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Operators)]
    public async Task<IActionResult> Update(int id, BookRequest request) => Ok(await _books.UpdateAsync(id, request));

    [HttpPost("{id:int}/withdraw"), Authorize(Roles = Roles.Librarian)]
    public async Task<IActionResult> Withdraw(int id, WithdrawRequest request) => Ok(await _books.WithdrawAsync(id, request));

    [HttpPost("{id:int}/restore"), Authorize(Roles = Roles.Librarian)]
    public async Task<IActionResult> Restore(int id) => Ok(await _books.RestoreAsync(id));

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Librarian)]
    public async Task<IActionResult> Delete(int id)
    {
        await _books.DeleteAsync(id);
        return NoContent();
    }

    // Move the old logbook or spreadsheet records in with one CSV file
    [HttpPost("import"), Authorize(Roles = Roles.Operators)]
    public async Task<IActionResult> Import(ImportRequest request) => Ok(await _books.ImportAsync(request.Csv));

    [HttpGet("export")]
    public async Task<IActionResult> Export() =>
        File(Encoding.UTF8.GetBytes(await _books.ExportCsvAsync()), "text/csv", "library-inventory.csv");
}
