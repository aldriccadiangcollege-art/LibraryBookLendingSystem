using LibraryBookLendingSystem.Models.DTOs.Auth;
using LibraryBookLendingSystem.Models.DTOs.Books;
using LibraryBookLendingSystem.Models.DTOs.Common;
using LibraryBookLendingSystem.Models.DTOs.Loans;
using LibraryBookLendingSystem.Models.DTOs.Members;
using LibraryBookLendingSystem.Models.DTOs.Reports;
using LibraryBookLendingSystem.Models.DTOs.Users;

namespace LibraryBookLendingSystem.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task ChangePasswordAsync(string username, ChangePasswordRequest request);
}

public interface IUserService
{
    Task<List<UserResponse>> ListAsync();
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task ResetPasswordAsync(int id, ResetPasswordRequest request);
    Task<UserResponse> SetActiveAsync(int id, bool isActive);
}

public interface IAuditService
{
    /// <summary>Adds an audit entry. It is saved together with the caller's next SaveAsync.</summary>
    void Record(string action, string details, string? username = null);
    Task SaveAsync();
}

public interface IBackupService
{
    Task<string> BackupAsync();
}

public interface IBookService
{
    Task<List<BookResponse>> GetAllAsync(string? q, string? category, bool availableOnly, bool includeWithdrawn);
    Task<List<string>> CategoriesAsync();
    Task<BookResponse> GetAsync(int id);
    Task<BookResponse> CreateAsync(BookRequest request);
    Task<BookResponse> UpdateAsync(int id, BookRequest request);
    Task<BookResponse> WithdrawAsync(int id, WithdrawRequest request);
    Task<BookResponse> RestoreAsync(int id);
    Task DeleteAsync(int id);
    Task<ImportResult> ImportAsync(string csv);
    Task<string> ExportCsvAsync();
}

public interface IMemberService
{
    Task<List<MemberResponse>> GetAllAsync(string? q);
    Task<MemberResponse> GetAsync(int id);
    Task<MemberResponse> CreateAsync(MemberRequest request);
    Task<MemberResponse> UpdateAsync(int id, MemberRequest request);
    Task DeleteAsync(int id);
    Task<ImportResult> ImportAsync(string csv);
}

public interface ILoanService
{
    Task<List<LoanResponse>> ListAsync(bool activeOnly);
    Task<List<LoanResponse>> HistoryAsync(int memberId);
    Task<List<LoanResponse>> UnpaidFinesAsync();
    Task<LoanResponse> BorrowAsync(BorrowRequest request);
    Task<LoanResponse> ReturnAsync(int loanId, ReturnRequest request);
    Task<LoanResponse> RenewAsync(int loanId);
    Task<LoanResponse> PayFineAsync(int loanId);
}

public interface IReportService
{
    Task<DashboardResponse> DashboardAsync();
    Task<List<LoanResponse>> BorrowersLogAsync(DateTime? from, DateTime? to);
    Task<List<UsageRow>> UsageAsync(DateTime? from, DateTime? to);
    Task<List<LoanResponse>> OverdueAsync();
    Task<List<FineRow>> FinesAsync();
    Task<List<ReviewRow>> CollectionReviewAsync();
    Task<List<DuplicateRow>> DuplicatesAsync();
    Task<List<WithdrawnRow>> WithdrawnAsync();
    Task<List<ShelfRow>> ShelfListAsync();
    Task<List<ReminderRow>> RemindersAsync();
    Task<List<AuditRow>> AuditLogAsync();
}
