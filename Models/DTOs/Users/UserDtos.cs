using System.ComponentModel.DataAnnotations;

namespace LibraryBookLendingSystem.Models.DTOs.Users;

public record UserResponse(int Id, string Username, string FullName, string Role, bool IsActive);

public class CreateUserRequest
{
    [Required, StringLength(50, MinimumLength = 3)] public string Username { get; set; } = "";
    [Required, StringLength(100)] public string FullName { get; set; } = "";

    [Required, RegularExpression("Librarian|LRCoordinator|Administrator", ErrorMessage = "Role must be Librarian, LRCoordinator or Administrator.")]
    public string Role { get; set; } = "";

    [Required, RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,100}$",
        ErrorMessage = "Password must be 8 to 100 characters with at least one letter and one number.")]
    public string Password { get; set; } = "";
}

public class ResetPasswordRequest
{
    [Required, RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).{8,100}$",
        ErrorMessage = "Password must be 8 to 100 characters with at least one letter and one number.")]
    public string NewPassword { get; set; } = "";
}
