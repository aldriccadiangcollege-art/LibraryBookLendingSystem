using LibraryBookLendingSystem.Data;
using LibraryBookLendingSystem.Models.Entities;
using LibraryBookLendingSystem.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryBookLendingSystem.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly LibraryDbContext _db;
    public UserRepository(LibraryDbContext db) => _db = db;

    public Task<User?> GetByUsernameAsync(string username) => _db.Users.FirstOrDefaultAsync(u => u.Username == username);
    public async Task<User?> GetAsync(int id) => await _db.Users.FindAsync(id);
    public Task<List<User>> ListAsync() => _db.Users.OrderBy(u => u.Username).ToListAsync();
    public Task<bool> UsernameExistsAsync(string username) => _db.Users.AnyAsync(u => u.Username == username);
    public void Add(User user) => _db.Users.Add(user);
    public Task SaveAsync() => _db.SaveChangesAsync();
}

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _db;
    public BookRepository(LibraryDbContext db) => _db = db;

    public Task<List<Book>> GetAllAsync(string? q, string? category, bool availableOnly, bool includeWithdrawn)
    {
        var query = _db.Books.AsQueryable();
        if (!includeWithdrawn) query = query.Where(b => b.Status == "Active");
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(b => b.Category == category);
        if (availableOnly) query = query.Where(b => b.AvailableCopies > 0);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(b => b.Title.Contains(q) || b.Author.Contains(q) || b.Category.Contains(q) || b.ShelfLocation.Contains(q));
        return query.OrderBy(b => b.Title).ToListAsync();
    }

    public async Task<Book?> GetAsync(int id) => await _db.Books.FindAsync(id);

    public Task<List<string>> CategoriesAsync() =>
        _db.Books.Where(b => b.Status == "Active").Select(b => b.Category).Distinct().OrderBy(c => c).ToListAsync();

    public Task<bool> ExistsAsync(string title, string author, int? exceptId) =>
        _db.Books.AnyAsync(b => b.Title == title && b.Author == author && b.Id != exceptId);

    public Task<bool> HasLoansAsync(int id) => _db.Loans.AnyAsync(l => l.BookId == id);
    public void Add(Book book) => _db.Books.Add(book);
    public void Remove(Book book) => _db.Books.Remove(book);
    public Task SaveAsync() => _db.SaveChangesAsync();
}

public class MemberRepository : IMemberRepository
{
    private readonly LibraryDbContext _db;
    public MemberRepository(LibraryDbContext db) => _db = db;

    public Task<List<Member>> GetAllAsync(string? q)
    {
        var query = _db.Members.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(m => m.FullName.Contains(q) || m.LibraryCardNo.Contains(q) ||
                                     (m.GradeSection != null && m.GradeSection.Contains(q)));
        return query.OrderBy(m => m.FullName).ToListAsync();
    }

    public async Task<Member?> GetAsync(int id) => await _db.Members.FindAsync(id);

    public Task<bool> CardExistsAsync(string cardNo, int? exceptId) =>
        _db.Members.AnyAsync(m => m.LibraryCardNo == cardNo && m.Id != exceptId);

    public Task<bool> HasLoansAsync(int id) => _db.Loans.AnyAsync(l => l.MemberId == id);
    public Task<int> CountActiveAsync() => _db.Members.CountAsync(m => m.IsActive);
    public void Add(Member member) => _db.Members.Add(member);
    public void Remove(Member member) => _db.Members.Remove(member);
    public Task SaveAsync() => _db.SaveChangesAsync();
}

public class LoanRepository : ILoanRepository
{
    private readonly LibraryDbContext _db;
    public LoanRepository(LibraryDbContext db) => _db = db;

    private IQueryable<Loan> Full => _db.Loans.Include(l => l.Book).Include(l => l.Member);

    public Task<Loan?> GetAsync(int id) => Full.FirstOrDefaultAsync(l => l.Id == id);

    public Task<List<Loan>> ListAsync(bool activeOnly) =>
        Full.Where(l => !activeOnly || l.ReturnDate == null)
            .OrderByDescending(l => l.LoanDate).Take(300).ToListAsync();

    public Task<List<Loan>> ByMemberAsync(int memberId) =>
        Full.Where(l => l.MemberId == memberId).OrderByDescending(l => l.LoanDate).ToListAsync();

    public Task<int> CountActiveAsync(int memberId) =>
        _db.Loans.CountAsync(l => l.MemberId == memberId && l.ReturnDate == null);

    public Task<bool> HasOverdueAsync(int memberId)
    {
        var today = DateTime.Today;
        return _db.Loans.AnyAsync(l => l.MemberId == memberId && l.ReturnDate == null && l.DueDate < today);
    }

    public Task<bool> HasUnpaidFineAsync(int memberId) =>
        _db.Loans.AnyAsync(l => l.MemberId == memberId && l.FineAmount > 0 && !l.FinePaid);

    public Task<List<Loan>> RangeAsync(DateTime? from, DateTime? to)
    {
        var q = Full;
        if (from.HasValue) { var start = from.Value.Date; q = q.Where(l => l.LoanDate >= start); }
        if (to.HasValue) { var end = to.Value.Date.AddDays(1); q = q.Where(l => l.LoanDate < end); }
        return q.OrderByDescending(l => l.LoanDate).ToListAsync();
    }

    public Task<List<Loan>> OverdueAsync()
    {
        var today = DateTime.Today;
        return Full.Where(l => l.ReturnDate == null && l.DueDate < today).OrderBy(l => l.DueDate).ToListAsync();
    }

    public Task<List<Loan>> DueByAsync(DateTime until) =>
        Full.Where(l => l.ReturnDate == null && l.DueDate <= until).OrderBy(l => l.DueDate).ToListAsync();

    public Task<List<Loan>> FinesAsync(bool unpaidOnly) =>
        Full.Where(l => l.FineAmount > 0 && (!unpaidOnly || !l.FinePaid)).OrderByDescending(l => l.ReturnDate).ToListAsync();

    public Task<int> CountBorrowedAsync() => _db.Loans.CountAsync(l => l.ReturnDate == null);

    public Task<int> CountOverdueAsync()
    {
        var today = DateTime.Today;
        return _db.Loans.CountAsync(l => l.ReturnDate == null && l.DueDate < today);
    }

    public Task<int> CountOnDayAsync(DateTime day)
    {
        var start = day.Date;
        var end = start.AddDays(1);
        return _db.Loans.CountAsync(l => (l.LoanDate >= start && l.LoanDate < end) ||
                                         (l.ReturnDate != null && l.ReturnDate >= start && l.ReturnDate < end));
    }

    public Task<decimal> UnpaidFinesTotalAsync() =>
        _db.Loans.Where(l => l.FineAmount > 0 && !l.FinePaid).SumAsync(l => l.FineAmount);

    public void Add(Loan loan) => _db.Loans.Add(loan);
    public Task SaveAsync() => _db.SaveChangesAsync();
}

public class AuditRepository : IAuditRepository
{
    private readonly LibraryDbContext _db;
    public AuditRepository(LibraryDbContext db) => _db = db;

    public void Add(AuditLog entry) => _db.AuditLogs.Add(entry);
    public Task<List<AuditLog>> RecentAsync(int count) =>
        _db.AuditLogs.OrderByDescending(a => a.Timestamp).ThenByDescending(a => a.Id).Take(count).ToListAsync();
    public Task SaveAsync() => _db.SaveChangesAsync();
}
