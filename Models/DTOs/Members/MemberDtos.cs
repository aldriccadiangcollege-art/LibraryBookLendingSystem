using System.ComponentModel.DataAnnotations;

namespace LibraryBookLendingSystem.Models.DTOs.Members;

public class MemberRequest
{
    [Required, StringLength(50)] public string LibraryCardNo { get; set; } = "";
    [Required, StringLength(150)] public string FullName { get; set; } = "";
    [StringLength(50)] public string? GradeSection { get; set; }
    [StringLength(30)] public string? ContactNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public record MemberResponse(int Id, string LibraryCardNo, string FullName, string? GradeSection, string? ContactNumber, bool IsActive);
