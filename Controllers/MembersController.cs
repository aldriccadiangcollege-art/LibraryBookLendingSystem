using LibraryBookLendingSystem.Models.DTOs.Common;
using LibraryBookLendingSystem.Models.DTOs.Members;
using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryBookLendingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Staff)]
public class MembersController : ControllerBase
{
    private readonly IMemberService _members;
    public MembersController(IMemberService members) => _members = members;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q) => Ok(await _members.GetAllAsync(q));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => Ok(await _members.GetAsync(id));

    // Only the Librarian manages borrower records
    [HttpPost, Authorize(Roles = Roles.Librarian)]
    public async Task<IActionResult> Create(MemberRequest request)
    {
        var member = await _members.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = member.Id }, member);
    }

    [HttpPut("{id:int}"), Authorize(Roles = Roles.Librarian)]
    public async Task<IActionResult> Update(int id, MemberRequest request) => Ok(await _members.UpdateAsync(id, request));

    [HttpDelete("{id:int}"), Authorize(Roles = Roles.Librarian)]
    public async Task<IActionResult> Delete(int id)
    {
        await _members.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("import"), Authorize(Roles = Roles.Librarian)]
    public async Task<IActionResult> Import(ImportRequest request) => Ok(await _members.ImportAsync(request.Csv));
}
