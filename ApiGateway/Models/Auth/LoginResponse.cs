namespace ApiGateway.Models.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool MustChangePassword { get; set; }

    public string? Specialization { get; set; }
    public string? HospitalAffiliation { get; set; }
}