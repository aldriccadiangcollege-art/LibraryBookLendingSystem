namespace LibraryBookLendingSystem.Models.Entities;

public static class Roles
{
    public const string Librarian = "Librarian";
    public const string LRCoordinator = "LRCoordinator";
    public const string Administrator = "Administrator";

    public const string Operators = Librarian + "," + LRCoordinator;                    // books, lending, imports
    public const string Reporters = Librarian + "," + Administrator;                    // reports, audit log, backup
    public const string Staff = Librarian + "," + LRCoordinator + "," + Administrator;  // any logged-in staff
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Role { get; set; } = Roles.Librarian;
    public bool IsActive { get; set; } = true;
    public int FailedAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
}

public class Book
{
    public const int OutdatedYears = 10;

    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public int PublicationYear { get; set; }
    public string ShelfLocation { get; set; } = "";
    public int TotalCopies { get; set; }        // copies owned (lost copies are removed)
    public int DamagedCopies { get; set; }      // owned but not lendable
    public int AvailableCopies { get; set; }    // on the shelf and lendable = Total - Damaged - borrowed
    public string ConditionNote { get; set; } = "";
    public string Status { get; set; } = "Active";   // Active or Withdrawn
    public string? WithdrawnReason { get; set; }     // Outdated, Duplicate, Damaged, Donated, Lost, Other
    public DateTime? WithdrawnDate { get; set; }
    public List<Loan> Loans { get; set; } = new();
}

public class Member
{
    public int Id { get; set; }
    public string LibraryCardNo { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? GradeSection { get; set; }
    public string? ContactNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Loan> Loans { get; set; } = new();
}

public class Loan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string? ReturnCondition { get; set; }     // Good, Damaged or Lost
    public int RenewalCount { get; set; }
    public decimal FineAmount { get; set; }
    public bool FinePaid { get; set; }
}

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Username { get; set; } = "";
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
}
