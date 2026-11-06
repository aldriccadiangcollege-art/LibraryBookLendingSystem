namespace LibraryBookLendingSystem.Models.DTOs.Reports;

public record UsageRow(string Date, int Borrowed, int Returned);

public record ReviewRow(int BookId, string Title, string Author, int PublicationYear, int TotalCopies, string Reason);

public record DuplicateRow(string Title, int Records, string Authors, string BookIds);

public record WithdrawnRow(int BookId, string Title, string Author, int PublicationYear, string Reason, string Date);

public record FineRow(int LoanId, string Borrower, string Book, string ReturnDate, decimal FineAmount, string Paid);

public record ReminderRow(int LoanId, string Borrower, string Contact, string Book, string DueDate, int DaysLate, string Status, string Message);

public record ShelfRow(string Shelf, string Title, string Author, string Category, int TotalCopies, int AvailableCopies, int DamagedCopies);

public record AuditRow(int Id, string Time, string User, string Action, string Details);

public record DashboardResponse(int Titles, int Copies, int Available, int BorrowedNow, int OverdueNow, int DamagedCopies,
    int OutdatedTitles, int WithdrawnTitles, int ActiveMembers, int TransactionsToday, decimal UnpaidFines);
