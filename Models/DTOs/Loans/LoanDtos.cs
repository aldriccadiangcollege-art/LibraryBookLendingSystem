using System.ComponentModel.DataAnnotations;

namespace LibraryBookLendingSystem.Models.DTOs.Loans;

public class BorrowRequest
{
    [Range(1, int.MaxValue)] public int BookId { get; set; }
    [Range(1, int.MaxValue)] public int MemberId { get; set; }
}

public class ReturnRequest
{
    [RegularExpression("Good|Damaged|Lost", ErrorMessage = "Condition must be Good, Damaged or Lost.")]
    public string Condition { get; set; } = "Good";
}

public record LoanResponse(int Id, int BookId, string BookTitle, int MemberId, string MemberName,
    DateTime LoanDate, DateTime DueDate, DateTime? ReturnDate, int RenewalCount, string? ReturnCondition,
    decimal FineAmount, bool FinePaid, string Status);
