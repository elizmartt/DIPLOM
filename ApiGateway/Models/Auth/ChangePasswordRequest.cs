using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models.Auth;

public class ChangePasswordRequest
{
    [Required]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string NewPassword { get; set; } = string.Empty;
}