namespace ApiGateway.Models.Auth;

public class DoctorListResponse
{
    public Guid DoctorId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Doctor";
    public string Specialization { get; set; } = string.Empty;
    public string HospitalAffiliation { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}