using System.ComponentModel.DataAnnotations;

namespace LibraryBookLendingSystem.Models.DTOs.Books;

public class BookRequest
{
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(150)] public string Author { get; set; } = "";
    [Required, StringLength(100)] public string Category { get; set; } = "";
    [Range(1800, 2100)] public int PublicationYear { get; set; }
    [StringLength(50)] public string ShelfLocation { get; set; } = "";
    [Range(1, 1000)] public int TotalCopies { get; set; }
    [Range(0, 1000)] public int DamagedCopies { get; set; }
    [StringLength(200)] public string ConditionNote { get; set; } = "";
}

public class WithdrawRequest
{
    [Required, RegularExpression("Outdated|Duplicate|Damaged|Donated|Lost|Other", ErrorMessage = "Reason must be Outdated, Duplicate, Damaged, Donated, Lost or Other.")]
    public string Reason { get; set; } = "";
}

public record BookResponse(int Id, string Title, string Author, string Category, int PublicationYear, string ShelfLocation,
    int TotalCopies, int AvailableCopies, int DamagedCopies, string Condition, string ConditionNote,
    string Status, string? WithdrawnReason, bool IsOutdated);
