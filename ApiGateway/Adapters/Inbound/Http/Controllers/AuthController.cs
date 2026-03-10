using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiGateway.Models;
using ApiGateway.Models.Auth;
using ApiGateway.Ports;

namespace ApiGateway.Adapters.Inbound.Http.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<LoginResponse>.FailureResult("Invalid request data", errors));
            }

            var response = await _authService.LoginAsync(request);
            if (response == null)
                return Unauthorized(ApiResponse<LoginResponse>.FailureResult("Invalid credentials"));

            _logger.LogInformation("User logged in: {Email}, Role: {Role}", response.Email, response.Role);
            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Login successful"));
        }

        [HttpPost("register-doctor")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> RegisterDoctor(
            [FromBody] RegisterDoctorRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<LoginResponse>.FailureResult("Invalid request data", errors));
            }

            var response = await _authService.RegisterDoctorAsync(request);
            if (response == null)
                return BadRequest(ApiResponse<LoginResponse>.FailureResult(
                    "Doctor registration failed. Credentials may already exist."));

            _logger.LogInformation("New doctor registered: {Email}", request.Email);
            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Doctor registered successfully"));
        }

        [HttpGet("me")]
        [Authorize]
        public ActionResult<ApiResponse<object>> GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var email  = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var name   = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role   = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            var userInfo = new { UserId = userId, Email = email, Name = name, Role = role };
            return Ok(ApiResponse<object>.SuccessResult(userInfo, "User info retrieved"));
        }

        // GET /api/Auth/doctors  — Admin only
        [HttpGet("doctors")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<DoctorListResponse>>>> GetAllDoctors()
        {
            var doctors = await _authService.GetAllDoctorsAsync();
            return Ok(ApiResponse<List<DoctorListResponse>>.SuccessResult(
                doctors, "Doctors retrieved successfully"));
        }
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
            [FromBody] ChangePasswordRequest request)
        {
            var doctorId = Guid.TryParse(
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                out var id) ? id : (Guid?)null;

            if (doctorId == null)
                return Unauthorized(ApiResponse<object>.FailureResult("Invalid credentials"));

            var doctor = await _authService.ChangePasswordAsync(doctorId.Value, request.NewPassword);
            if (doctor == null)
                return BadRequest(ApiResponse<object>.FailureResult("Password change failed"));

            return Ok(ApiResponse<object>.SuccessResult(null, "Password changed successfully"));
        }
    }
}