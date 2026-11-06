using LibraryBookLendingSystem.Exceptions;
using LibraryBookLendingSystem.Models;
using LibraryBookLendingSystem.Models.DTOs.Loans;
using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Repositories.Interfaces;
using LibraryBookLendingSystem.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace LibraryBookLendingSystem.Services.Implementations;

public class LoanService : ILoanService
{
    private readonly IBookRepository _books;
    private readonly IMemberRepository _members;
    private readonly ILoanRepository _loans;
    private readonly IAuditService _audit;
    private readonly LibrarySettings _rules;

    public LoanService(IBookRepository books, IMemberRepository members, ILoanRepository loans,
        IAuditService audit, IOptions<LibrarySettings> rules)
    {
        _books = books;
        _members = members;
        _loans = loans;
        _audit = audit;
        _rules = rules.Value;
    }

    public static LoanResponse ToResponse(Loan l)
    {
        var status = l.ReturnDate != null ? (l.ReturnCondition == "Lost" ? "Lost" : "Returned")
            : l.DueDate.Date < DateTime.Today ? "Overdue" : "Borrowed";
        return new LoanResponse(l.Id, l.BookId, l.Book.Title, l.MemberId, l.Member.FullName, l.LoanDate, l.DueDate,
            l.ReturnDate, l.RenewalCount, l.ReturnCondition, l.FineAmount, l.FinePaid, status);
    }

    public async Task<List<LoanResponse>> ListAsync(bool activeOnly) =>
        (await _loans.ListAsync(activeOnly)).Select(ToResponse).ToList();

    public async Task<List<LoanResponse>> HistoryAsync(int memberId)
    {
        _ = await _members.GetAsync(memberId) ?? throw new NotFoundException($"Member {memberId} was not found.");
        return (await _loans.ByMemberAsync(memberId)).Select(ToResponse).ToList();
    }

    public async Task<List<LoanResponse>> UnpaidFinesAsync() =>
        (await _loans.FinesAsync(unpaidOnly: true)).Select(ToResponse).ToList();

    public async Task<LoanResponse> BorrowAsync(BorrowRequest r)
    {
        var book = await _books.GetAsync(r.BookId) ?? throw new NotFoundException($"Book {r.BookId} was not found.");
        var member = await _members.GetAsync(r.MemberId) ?? throw new NotFoundException($"Member {r.MemberId} was not found.");

        if (!member.IsActive) throw new ConflictException("This member is inactive and cannot borrow books.");
        if (book.Status != "Active") throw new ConflictException("This book was withdrawn from the collection.");
        if (book.AvailableCopies < 1) throw new ConflictException("No lendable copies of this book are available right now.");
        if (await _loans.HasUnpaidFineAsync(member.Id))
            throw new ConflictException("This member has an unpaid fine. Record the payment first.");
        if (await _loans.HasOverdueAsync(member.Id))
            throw new ConflictException("This member has an overdue book. It must be returned first.");

        var active = await _loans.CountActiveAsync(member.Id);
        if (active >= _rules.MaxBooksPerMember)
            throw new ConflictException($"This member already has {active} unreturned book(s). The limit is {_rules.MaxBooksPerMember}.");

        var now = DateTime.Now;
        var loan = new Loan { Book = book, Member = member, LoanDate = now, DueDate = now.Date.AddDays(_rules.LoanDays) };
        book.AvailableCopies--;
        _loans.Add(loan);
        _audit.Record("Borrow", $"'{book.Title}' to {member.FullName} ({member.LibraryCardNo}), due {loan.DueDate:yyyy-MM-dd}.");
        await _loans.SaveAsync();
        return ToResponse(loan);
    }

    public async Task<LoanResponse> ReturnAsync(int loanId, ReturnRequest r)
    {
        var loan = await _loans.GetAsync(loanId) ?? throw new NotFoundException($"Loan {loanId} was not found.");
        if (loan.ReturnDate != null) throw new ConflictException("This loan was already returned.");

        loan.ReturnDate = DateTime.Now;
        loan.ReturnCondition = r.Condition;

        var lateDays = (loan.ReturnDate.Value.Date - loan.DueDate.Date).Days - _rules.GracePeriodDays;
        loan.FineAmount = lateDays > 0 ? lateDays * _rules.FinePerDay : 0m;

        switch (r.Condition)
        {
            case "Good": loan.Book.AvailableCopies++; break;      // back on the shelf
            case "Damaged": loan.Book.DamagedCopies++; break;     // kept, but out of circulation
            case "Lost": loan.Book.TotalCopies--; break;          // copy removed from the collection
        }

        _audit.Record("Return", $"'{loan.Book.Title}' from {loan.Member.FullName}: {r.Condition}, fine {loan.FineAmount:0.00}.");
        await _loans.SaveAsync();
        return ToResponse(loan);
    }

    public async Task<LoanResponse> RenewAsync(int loanId)
    {
        var loan = await _loans.GetAsync(loanId) ?? throw new NotFoundException($"Loan {loanId} was not found.");
        if (loan.ReturnDate != null) throw new ConflictException("This loan was already returned.");
        if (loan.DueDate.Date < DateTime.Today) throw new ConflictException("Overdue loans cannot be renewed. Return the book first.");
        if (loan.RenewalCount >= _rules.MaxRenewals)
            throw new ConflictException($"This loan was already renewed {loan.RenewalCount} time(s). The limit is {_rules.MaxRenewals}.");

        loan.DueDate = loan.DueDate.Date.AddDays(_rules.LoanDays);
        loan.RenewalCount++;
        _audit.Record("Renew", $"'{loan.Book.Title}' for {loan.Member.FullName}, new due date {loan.DueDate:yyyy-MM-dd}.");
        await _loans.SaveAsync();
        return ToResponse(loan);
    }

    public async Task<LoanResponse> PayFineAsync(int loanId)
    {
        var loan = await _loans.GetAsync(loanId) ?? throw new NotFoundException($"Loan {loanId} was not found.");
        if (loan.FineAmount <= 0) throw new ConflictException("This loan has no fine.");
        if (loan.FinePaid) throw new ConflictException("This fine was already paid.");

        loan.FinePaid = true;
        _audit.Record("FinePaid", $"{loan.FineAmount:0.00} from {loan.Member.FullName} for '{loan.Book.Title}'.");
        await _loans.SaveAsync();
        return ToResponse(loan);
    }
}
