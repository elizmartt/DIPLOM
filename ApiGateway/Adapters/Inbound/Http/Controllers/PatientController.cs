using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiGateway.Models;
using ApiGateway.Models.Patient;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Entities;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Services;

namespace ApiGateway.Adapters.Inbound.Http.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor,Admin")]
    public class PatientController : ControllerBase
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<PatientController> _logger;
        private readonly IEncryptionService _encryption;

        public PatientController(
            IPatientRepository patientRepository,
            IAuditLogRepository auditLogRepository,
            ILogger<PatientController> logger,
            IEncryptionService encryption)
        {
            _patientRepository = patientRepository;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
            _encryption = encryption;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<PatientResponse>>> CreatePatient(
            [FromBody] CreatePatientRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<PatientResponse>.FailureResult("Invalid request data", errors));
            }

            try
            {
                var patientCode = await GenerateUniquePatientCodeAsync();

                var patient = new Patient
                {
                    PatientId   = Guid.NewGuid(),
                    PatientCode = patientCode,
                    FirstName   = _encryption.Encrypt(request.FirstName),
                    LastName    = _encryption.Encrypt(request.LastName),
                    Age         = request.Age,
                    Gender      = request.Gender,
                    CreatedAt   = DateTime.UtcNow,
                    UpdatedAt   = DateTime.UtcNow
                };

                var createdPatient = await _patientRepository.CreateAsync(patient);

                var doctorId = GetDoctorIdFromClaims();
                await _auditLogRepository.LogActionAsync(
                    action: "CREATE_PATIENT",
                    doctorId: doctorId,
                    entityType: "Patient",
                    entityId: createdPatient.PatientId,
                    actionDetails: new Dictionary<string, object>
                    {
                        { "PatientCode", patientCode },
                        { "Age", request.Age },
                        { "Gender", request.Gender }
                    },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString()
                );

                _logger.LogInformation("Patient created: {PatientCode} by Doctor: {DoctorId}", patientCode, doctorId);

                return Ok(ApiResponse<PatientResponse>.SuccessResult(
                    MapToResponse(createdPatient), "Patient created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                return StatusCode(500, ApiResponse<PatientResponse>.FailureResult(
                    "An error occurred while creating the patient"));
            }
        }

        [HttpGet("{patientId}")]
        public async Task<ActionResult<ApiResponse<PatientResponse>>> GetPatient(Guid patientId)
        {
            try
            {
                var patient = await _patientRepository.GetByIdAsync(patientId);
                if (patient == null)
                    return NotFound(ApiResponse<PatientResponse>.FailureResult("Patient not found"));

                return Ok(ApiResponse<PatientResponse>.SuccessResult(MapToResponse(patient)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient: {PatientId}", patientId);
                return StatusCode(500, ApiResponse<PatientResponse>.FailureResult(
                    "An error occurred while retrieving the patient"));
            }
        }

        [HttpGet("by-code/{patientCode}")]
        public async Task<ActionResult<ApiResponse<PatientResponse>>> GetPatientByCode(string patientCode)
        {
            try
            {
                var patient = await _patientRepository.GetByPatientCodeAsync(patientCode);
                if (patient == null)
                    return NotFound(ApiResponse<PatientResponse>.FailureResult("Patient not found"));

                return Ok(ApiResponse<PatientResponse>.SuccessResult(MapToResponse(patient)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient by code: {PatientCode}", patientCode);
                return StatusCode(500, ApiResponse<PatientResponse>.FailureResult(
                    "An error occurred while retrieving the patient"));
            }
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PatientResponse>>>> ListPatients(
            [FromQuery] int limit = 50)
        {
            try
            {
                if (limit > 100) limit = 100;

                var patients = await _patientRepository.GetAllAsync(limit);
                var response = patients.Select(MapToResponse).ToList();

                return Ok(ApiResponse<List<PatientResponse>>.SuccessResult(
                    response, $"Retrieved {response.Count} patients"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing patients");
                return StatusCode(500, ApiResponse<List<PatientResponse>>.FailureResult(
                    "An error occurred while retrieving patients"));
            }
        }


        private async Task<string> GenerateUniquePatientCodeAsync()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"PAT-{datePart}-";

            var allPatients = await _patientRepository.GetAllAsync(10000);
            var maxToday = allPatients
                .Where(p => p.PatientCode?.StartsWith(prefix) == true)
                .Select(p =>
                {
                    var suffix = p.PatientCode!.Substring(prefix.Length);
                    return int.TryParse(suffix, out var n) ? n : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            var next = maxToday + 1;
            string code;
            do
            {
                code = $"{prefix}{next:D3}";
                next++;
            } while (await _patientRepository.GetByPatientCodeAsync(code) != null);

            return code;
        }

        private Guid? GetDoctorIdFromClaims()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }


        private string SafeDecrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            try
            {
                return _encryption.Decrypt(value);
            }
            catch (FormatException)
            {
                return value;
            }
            catch (CryptographicException)
            {
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Decrypt failed, returning as-is");
                return value;
            }
        }

        private PatientResponse MapToResponse(Patient patient)
        {
            return new PatientResponse
            {
                PatientId    = patient.PatientId,
                PatientCode  = patient.PatientCode,
                FirstName    = SafeDecrypt(patient.FirstName),
                LastName     = SafeDecrypt(patient.LastName),
                Age          = patient.Age,
                Gender       = patient.Gender,
                CreatedAt    = patient.CreatedAt,
                UpdatedAt    = patient.UpdatedAt,
            };
        }
    }
}