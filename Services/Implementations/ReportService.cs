using LibraryBookLendingSystem.Models;
using LibraryBookLendingSystem.Models.DTOs.Loans;
using LibraryBookLendingSystem.Models.DTOs.Reports;
using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Repositories.Interfaces;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace LibraryBookLendingSystem.Services.Implementations;

public class ReportService : IReportService
{
    private readonly ILoanRepository _loans;
    private readonly IBookRepository _books;
    private readonly IMemberRepository _members;
    private readonly IAuditRepository _audit;
    private readonly LibrarySettings _rules;

    public ReportService(ILoanRepository loans, IBookRepository books, IMemberRepository members,
        IAuditRepository audit, IOptions<LibrarySettings> rules)
    {
        _loans = loans;
        _books = books;
        _members = members;
        _audit = audit;
        _rules = rules.Value;
    }

    private static bool IsOutdated(Book b) => DateTime.Now.Year - b.PublicationYear >= Book.OutdatedYears;

    /// <summary>Numbers for the monitoring page of the librarian, LR Coordinator and administrators.</summary>
    public async Task<DashboardResponse> DashboardAsync()
    {
        var all = await _books.GetAllAsync(null, null, false, true);
        var active = all.Where(b => b.Status == "Active").ToList();
        return new DashboardResponse(
            active.Count, active.Sum(b => b.TotalCopies), active.Sum(b => b.AvailableCopies),
            await _loans.CountBorrowedAsync(), await _loans.CountOverdueAsync(),
            active.Sum(b => b.DamagedCopies), active.Count(IsOutdated), all.Count - active.Count,
            await _members.CountActiveAsync(), await _loans.CountOnDayAsync(DateTime.Today),
            await _loans.UnpaidFinesTotalAsync());
    }

    /// <summary>Borrowers track log: every loan in the period.</summary>
    public async Task<List<LoanResponse>> BorrowersLogAsync(DateTime? from, DateTime? to) =>
        (await _loans.RangeAsync(from, to)).Select(LoanService.ToResponse).ToList();

    /// <summary>List of library use: books borrowed and returned per day.</summary>
    public async Task<List<UsageRow>> UsageAsync(DateTime? from, DateTime? to)
    {
        var loans = await _loans.RangeAsync(from, to);
        return loans.GroupBy(l => l.LoanDate.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new UsageRow(g.Key.ToString("yyyy-MM-dd"), g.Count(), g.Count(x => x.ReturnDate != null)))
            .ToList();
    }

    public async Task<List<LoanResponse>> OverdueAsync() =>
        (await _loans.OverdueAsync()).Select(LoanService.ToResponse).ToList();

    public async Task<List<FineRow>> FinesAsync() =>
        (await _loans.FinesAsync(unpaidOnly: false)).Select(l => new FineRow(
            l.Id, l.Member.FullName, l.Book.Title, l.ReturnDate?.ToString("yyyy-MM-dd") ?? "", l.FineAmount,
            l.FinePaid ? "Paid" : "Unpaid")).ToList();

    /// <summary>Books to replace, repair or review: outdated, damaged, or with many copies.</summary>
    public async Task<List<ReviewRow>> CollectionReviewAsync()
    {
        var year = DateTime.Now.Year;
        var rows = new List<ReviewRow>();
        foreach (var b in await _books.GetAllAsync(null, null, false, false))
        {
            var reasons = new List<string>();
            if (IsOutdated(b)) reasons.Add($"Outdated ({year - b.PublicationYear} years old)");
            if (b.DamagedCopies > 0) reasons.Add($"{b.DamagedCopies} damaged cop(ies)");
            if (b.TotalCopies >= _rules.ExcessCopyThreshold) reasons.Add($"{b.TotalCopies} copies - check for extra copies");
            if (reasons.Count > 0)
                rows.Add(new ReviewRow(b.Id, b.Title, b.Author, b.PublicationYear, b.TotalCopies, string.Join("; ", reasons)));
        }
        return rows;
    }

    /// <summary>Possible duplicate records: same title once punctuation and capitals are ignored.</summary>
    public async Task<List<DuplicateRow>> DuplicatesAsync()
    {
        static string Norm(string s) => new string(s.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        var books = await _books.GetAllAsync(null, null, false, false);
        return books.GroupBy(b => Norm(b.Title)).Where(g => g.Count() > 1)
            .Select(g => new DuplicateRow(g.First().Title, g.Count(),
                string.Join("; ", g.Select(x => x.Author)), string.Join(", ", g.Select(x => x.Id))))
            .ToList();
    }

    /// <summary>Books taken out of the collection (outdated, duplicate, damaged, donated, lost). Basis for replacement.</summary>
    public async Task<List<WithdrawnRow>> WithdrawnAsync() =>
        (await _books.GetAllAsync(null, null, false, true)).Where(b => b.Status == "Withdrawn")
            .OrderByDescending(b => b.WithdrawnDate)
            .Select(b => new WithdrawnRow(b.Id, b.Title, b.Author, b.PublicationYear, b.WithdrawnReason ?? "",
                b.WithdrawnDate?.ToString("yyyy-MM-dd") ?? ""))
            .ToList();

    /// <summary>Books in shelf order, to speed up arranging and stock-taking.</summary>
    public async Task<List<ShelfRow>> ShelfListAsync() =>
        (await _books.GetAllAsync(null, null, false, false))
            .OrderBy(b => b.ShelfLocation).ThenBy(b => b.Title)
            .Select(b => new ShelfRow(b.ShelfLocation, b.Title, b.Author, b.Category, b.TotalCopies, b.AvailableCopies, b.DamagedCopies))
            .ToList();

    /// <summary>Due-soon and overdue loans with a ready-to-send message for the borrower.</summary>
    public async Task<List<ReminderRow>> RemindersAsync()
    {
        var today = DateTime.Today;
        var loans = await _loans.DueByAsync(today.AddDays(_rules.DueSoonDays));
        return loans.Select(l =>
        {
            var due = l.DueDate.Date;
            var late = (today - due).Days;
            var overdue = late > 0;
            var fine = Math.Max(0, late - _rules.GracePeriodDays) * _rules.FinePerDay;
            var message = overdue
                ? $"Good day, {l.Member.FullName}. The book \"{l.Book.Title}\" was due on {due:MMM d, yyyy}. Please return it to the MBASH - Unit I library." +
                  (fine > 0 ? $" Fine so far: PHP {fine:0.00}." : "")
                : $"Good day, {l.Member.FullName}. Reminder: the book \"{l.Book.Title}\" is due on {due:MMM d, yyyy}. Please return it or ask to renew it.";
            return new ReminderRow(l.Id, l.Member.FullName, l.Member.ContactNumber ?? "", l.Book.Title,
                due.ToString("yyyy-MM-dd"), Math.Max(0, late), overdue ? "Overdue" : "Due soon", message);
        }).ToList();
    }

    public async Task<List<AuditRow>> AuditLogAsync() =>
        (await _audit.RecentAsync(300)).Select(a => new AuditRow(a.Id, a.Timestamp.ToString("yyyy-MM-dd HH:mm"), a.Username, a.Action, a.Details)).ToList();
}
