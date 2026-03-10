using ApiGateway.Models.Auth;

namespace ApiGateway.Ports
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<LoginResponse?> RegisterDoctorAsync(RegisterDoctorRequest request);
        Task<List<DoctorListResponse>> GetAllDoctorsAsync();
        Task<bool> ChangePasswordAsync(Guid doctorId, string newPassword);
    }
}
