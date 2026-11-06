using System.ComponentModel.DataAnnotations;

namespace LibraryBookLendingSystem.Models.DTOs.Auth;

public class LoginRequest
{
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public record LoginResponse(string Token, string Username, string FullName, string Role, DateTime ExpiresAt);

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = "";

    [Required, RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,100}$",
        ErrorMessage = "New password must be 8 to 100 characters with at least one letter and one number.")]
    public string NewPassword { get; set; } = "";
}
