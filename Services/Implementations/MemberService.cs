using LibraryBookLendingSystem.Exceptions;
using LibraryBookLendingSystem.Helpers;
using LibraryBookLendingSystem.Models.DTOs.Common;
using LibraryBookLendingSystem.Models.DTOs.Members;
using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Repositories.Interfaces;
using LibraryBookLendingSystem.Services.Interfaces;

namespace LibraryBookLendingSystem.Services.Implementations;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _members;
    private readonly IAuditService _audit;

    public MemberService(IMemberRepository members, IAuditService audit)
    {
        _members = members;
        _audit = audit;
    }

    private static MemberResponse ToResponse(Member m) => new(m.Id, m.LibraryCardNo, m.FullName, m.GradeSection, m.ContactNumber, m.IsActive);

    public async Task<List<MemberResponse>> GetAllAsync(string? q) =>
        (await _members.GetAllAsync(q)).Select(ToResponse).ToList();

    public async Task<MemberResponse> GetAsync(int id) =>
        ToResponse(await _members.GetAsync(id) ?? throw new NotFoundException($"Member {id} was not found."));

    public async Task<MemberResponse> CreateAsync(MemberRequest r)
    {
        var card = r.LibraryCardNo.Trim();
        if (await _members.CardExistsAsync(card, null))
            throw new ConflictException($"Library card number {card} is already registered.");

        var member = new Member
        {
            LibraryCardNo = card, FullName = r.FullName.Trim(), GradeSection = r.GradeSection?.Trim(),
            ContactNumber = r.ContactNumber?.Trim(), IsActive = r.IsActive
        };
        _members.Add(member);
        _audit.Record("MemberAdded", $"{member.FullName} ({card}).");
        await _members.SaveAsync();
        return ToResponse(member);
    }

    public async Task<MemberResponse> UpdateAsync(int id, MemberRequest r)
    {
        var member = await _members.GetAsync(id) ?? throw new NotFoundException($"Member {id} was not found.");
        var card = r.LibraryCardNo.Trim();
        if (await _members.CardExistsAsync(card, id))
            throw new ConflictException($"Library card number {card} belongs to another member.");

        member.LibraryCardNo = card;
        member.FullName = r.FullName.Trim();
        member.GradeSection = r.GradeSection?.Trim();
        member.ContactNumber = r.ContactNumber?.Trim();
        member.IsActive = r.IsActive;
        _audit.Record("MemberUpdated", $"{member.FullName} ({card}), active: {member.IsActive}.");
        await _members.SaveAsync();
        return ToResponse(member);
    }

    public async Task DeleteAsync(int id)
    {
        var member = await _members.GetAsync(id) ?? throw new NotFoundException($"Member {id} was not found.");
        if (await _members.HasLoansAsync(id))
            throw new ConflictException("This member has lending records and cannot be deleted. Set the member as inactive instead.");
        _members.Remove(member);
        _audit.Record("MemberDeleted", $"{member.FullName} ({member.LibraryCardNo}).");
        await _members.SaveAsync();
    }

    /// <summary>Columns: LibraryCardNo, FullName, GradeSection, ContactNumber.</summary>
    public async Task<ImportResult> ImportAsync(string csv)
    {
        var rows = CsvUtil.Parse(csv);
        if (rows.Count > 0 && rows[0][0].Equals("LibraryCardNo", StringComparison.OrdinalIgnoreCase)) rows.RemoveAt(0);

        var added = 0;
        var skipped = 0;
        var messages = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            string Get(int idx) => idx < r.Length ? r[idx] : "";
            var n = i + 1;
            var card = Get(0);
            var name = Get(1);

            if (card == "" || name == "") { skipped++; messages.Add($"Row {n}: card number and name are required."); continue; }
            if (card.Length > 50 || name.Length > 150 || Get(2).Length > 50 || Get(3).Length > 30)
            { skipped++; messages.Add($"Row {n}: a value is too long."); continue; }
            if (!seen.Add(card) || await _members.CardExistsAsync(card, null))
            { skipped++; messages.Add($"Row {n}: card number {card} is already registered."); continue; }

            _members.Add(new Member
            {
                LibraryCardNo = card, FullName = name,
                GradeSection = Get(2) == "" ? null : Get(2),
                ContactNumber = Get(3) == "" ? null : Get(3)
            });
            added++;
        }

        if (added > 0)
        {
            _audit.Record("MemberImport", $"{added} added, {skipped} skipped.");
            await _members.SaveAsync();
        }
        return new ImportResult(added, skipped, messages);
    }
}
