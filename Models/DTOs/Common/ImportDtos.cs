using System.ComponentModel.DataAnnotations;

namespace LibraryBookLendingSystem.Models.DTOs.Common;

public class ImportRequest
{
    [Required] public string Csv { get; set; } = "";
}

public record ImportResult(int Added, int Skipped, List<string> Messages);
