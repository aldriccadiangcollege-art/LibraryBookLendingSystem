namespace LibraryBookLendingSystem.Models;

/// <summary>Library rules. Change the values in appsettings.json to match the school's policy.</summary>
public class LibrarySettings
{
    public int LoanDays { get; set; } = 7;
    public int MaxBooksPerMember { get; set; } = 3;
    public int MaxRenewals { get; set; } = 1;
    public int GracePeriodDays { get; set; } = 1;
    public decimal FinePerDay { get; set; } = 1.00m;
    public int DueSoonDays { get; set; } = 2;
    public int ExcessCopyThreshold { get; set; } = 3;
    public string BackupFolder { get; set; } = "C:\\LibraryBackups";
}
