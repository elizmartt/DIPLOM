using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using ApiGateway.Models.Auth;
using ApiGateway.Ports;
using ApiGateway.Configuration;
using Microsoft.Extensions.Options;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Entities;

namespace ApiGateway.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IDoctorRepository doctorRepository,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _doctorRepository = doctorRepository;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            try
            {
                var doctor = await _doctorRepository.GetByEmailAsync(request.Email);

                if (doctor == null)
                {
                    _logger.LogWarning("Login attempt for non-existent email: {Email}", request.Email);
                    return null;
                }

                if (!doctor.IsActive)
                {
                    _logger.LogWarning("Login attempt for inactive doctor: {Email}", request.Email);
                    return null;
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, doctor.PasswordHash))
                {
                    _logger.LogWarning("Invalid password attempt for: {Email}", request.Email);
                    return null;
                }

                return GenerateToken(doctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return null;
            }
        }

        public async Task<LoginResponse?> RegisterDoctorAsync(RegisterDoctorRequest request)
        {
            try
            {
                var existing = await _doctorRepository.GetByEmailAsync(request.Email);
                if (existing != null)
                {
                    _logger.LogWarning("Attempt to register existing doctor: {Email}", request.Email);
                    return null;
                }

                var doctor = new Doctor
                {
                    DoctorId            = Guid.NewGuid(),
                    Email               = request.Email,
                    FullName            = request.FullName,
                    Specialization      = request.Specialization,
                    HospitalAffiliation = request.HospitalAffiliation,
                    PasswordHash        = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Role                = "Doctor",
                    IsActive            = true,
                    CreatedAt           = DateTime.UtcNow,
                    MustChangePassword = true, 
                    UpdatedAt           = DateTime.UtcNow
                };

                var createdDoctor = await _doctorRepository.CreateAsync(doctor);
                _logger.LogInformation("Doctor registered successfully: {Email}", request.Email);
                return GenerateToken(createdDoctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering doctor: {Email}", request.Email);
                return null;
            }
        }

        public async Task<List<DoctorListResponse>> GetAllDoctorsAsync()
        {
            var doctors = await _doctorRepository.GetAllAsync();
            return doctors.Select(d => new DoctorListResponse
            {
                DoctorId            = d.DoctorId,
                Email               = d.Email,
                FullName            = d.FullName,
                Role                = d.Role,
                Specialization      = d.Specialization,
                HospitalAffiliation = d.HospitalAffiliation,
                IsActive            = d.IsActive,
                CreatedAt           = d.CreatedAt,
            }).ToList();
        }

        private LoginResponse GenerateToken(Doctor doctor)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, doctor.DoctorId.ToString()),
                new Claim(ClaimTypes.Email,          doctor.Email),
                new Claim(ClaimTypes.Name,           doctor.FullName),
                new Claim(ClaimTypes.Role,           doctor.Role),
                new Claim("Specialization",          doctor.Specialization ?? ""),
                new Claim("HospitalAffiliation",     doctor.HospitalAffiliation ?? "")
            };

            var token     = GenerateJwtToken(claims);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

            return new LoginResponse
            {
                Token               = token,
                Email               = doctor.Email,
                FullName            = doctor.FullName,
                Role                = doctor.Role,
                UserId              = doctor.DoctorId,
                ExpiresAt           = expiresAt,
                Specialization      = doctor.Specialization,
                MustChangePassword = doctor.MustChangePassword,
                HospitalAffiliation = doctor.HospitalAffiliation
            };
        }
        public async Task<bool> ChangePasswordAsync(Guid doctorId, string newPassword)
        {
            var doctor = await _doctorRepository.GetByIdAsync(doctorId);
            if (doctor == null) return false;

            doctor.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(newPassword);
            doctor.MustChangePassword  = false;
            doctor.UpdatedAt           = DateTime.UtcNow;

            await _doctorRepository.UpdateAsync(doctor);
            return true;
        }
        private string GenerateJwtToken(Claim[] claims)
        {
            var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer:             _jwtSettings.Issuer,
                audience:           _jwtSettings.Audience,
                claims:             claims,
                expires:            DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}