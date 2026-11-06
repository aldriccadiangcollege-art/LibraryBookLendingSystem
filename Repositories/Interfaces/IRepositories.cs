using LibraryBookLendingSystem.Models.Entities;

namespace LibraryBookLendingSystem.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetAsync(int id);
    Task<List<User>> ListAsync();
    Task<bool> UsernameExistsAsync(string username);
    void Add(User user);
    Task SaveAsync();
}

public interface IBookRepository
{
    Task<List<Book>> GetAllAsync(string? q, string? category, bool availableOnly, bool includeWithdrawn);
    Task<Book?> GetAsync(int id);
    Task<List<string>> CategoriesAsync();
    Task<bool> ExistsAsync(string title, string author, int? exceptId);
    Task<bool> HasLoansAsync(int id);
    void Add(Book book);
    void Remove(Book book);
    Task SaveAsync();
}

public interface IMemberRepository
{
    Task<List<Member>> GetAllAsync(string? q);
    Task<Member?> GetAsync(int id);
    Task<bool> CardExistsAsync(string cardNo, int? exceptId);
    Task<bool> HasLoansAsync(int id);
    Task<int> CountActiveAsync();
    void Add(Member member);
    void Remove(Member member);
    Task SaveAsync();
}

public interface ILoanRepository
{
    Task<Loan?> GetAsync(int id);
    Task<List<Loan>> ListAsync(bool activeOnly);
    Task<List<Loan>> ByMemberAsync(int memberId);
    Task<int> CountActiveAsync(int memberId);
    Task<bool> HasOverdueAsync(int memberId);
    Task<bool> HasUnpaidFineAsync(int memberId);
    Task<List<Loan>> RangeAsync(DateTime? from, DateTime? to);
    Task<List<Loan>> OverdueAsync();
    Task<List<Loan>> DueByAsync(DateTime until);
    Task<List<Loan>> FinesAsync(bool unpaidOnly);
    Task<int> CountBorrowedAsync();
    Task<int> CountOverdueAsync();
    Task<int> CountOnDayAsync(DateTime day);
    Task<decimal> UnpaidFinesTotalAsync();
    void Add(Loan loan);
    Task SaveAsync();
}

public interface IAuditRepository
{
    void Add(AuditLog entry);
    Task<List<AuditLog>> RecentAsync(int count);
    Task SaveAsync();
}
