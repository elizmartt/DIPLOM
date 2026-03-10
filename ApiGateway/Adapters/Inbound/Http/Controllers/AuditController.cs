using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiGateway.Models;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces;
using System.Text.Json;

namespace ApiGateway.Adapters.Inbound.Http.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Doctor,Admin")]
public class AuditController : ControllerBase
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly ILogger<AuditController> _logger;

    public AuditController(
        IAuditLogRepository auditLogRepository,
        IDoctorRepository doctorRepository,
        ILogger<AuditController> logger)
    {
        _auditLogRepository = auditLogRepository;
        _doctorRepository = doctorRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<object>>>> ListAuditLogs(
        [FromQuery] string? action = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] int limit = 200)
    {
        try
        {
            var role     = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var doctorId = GetDoctorIdFromClaims();

            if (limit > 500) limit = 500;

            // Admin sees all logs, doctor sees only their own
            IEnumerable<dynamic> filtered;
            if (role == "Admin")
            {
                var allLogs = await _auditLogRepository.GetRecentAsync(limit);
                filtered = allLogs.Cast<dynamic>();
            }
            else
            {
                if (doctorId == null)
                    return Unauthorized(ApiResponse<List<object>>.FailureResult("Invalid credentials"));
                var doctorLogs = await _auditLogRepository.GetByDoctorIdAsync(doctorId.Value, limit);
                filtered = doctorLogs.Cast<dynamic>();
            }

            if (!string.IsNullOrEmpty(action))
                filtered = filtered.Where(l => l.Action.Equals(action, StringComparison.OrdinalIgnoreCase));

            if (DateTime.TryParse(dateFrom, out var from))
                filtered = filtered.Where(l => l.CreatedAt >= from);

            if (DateTime.TryParse(dateTo, out var to))
                filtered = filtered.Where(l => l.CreatedAt < to.AddDays(1));

            var logList = filtered.Take(limit).ToList();

            // Build a doctor lookup map for all unique non-null doctor IDs in the logs
            var doctorIds = logList
                .Where(l => l.DoctorId != null)
                .Select(l => (Guid)l.DoctorId)
                .Distinct()
                .ToList();

            var doctorMap = new Dictionary<Guid, object?>();
            foreach (var id in doctorIds)
            {
                var d = await _doctorRepository.GetByIdAsync(id);
                doctorMap[id] = d == null ? null : new
                {
                    full_name = d.FullName,
                    email     = d.Email,
                };
            }

            var result = logList.Select(l => (object)new
            {
                log_id         = l.LogId,
                created_at     = l.CreatedAt,
                doctor_id      = l.DoctorId,
                case_id        = l.CaseId,
                action         = l.Action,
                entity_type    = l.EntityType,
                entity_id      = l.EntityId,
                action_details = l.ActionDetails != null
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(l.ActionDetails)
                    : null,
                ip_address     = l.IpAddress,
                user_agent     = l.UserAgent,
                doctor = l.DoctorId != null
                    ? (doctorMap.TryGetValue((Guid)l.DoctorId, out var doc) ? doc : null)
                    : null,
            }).ToList();

            return Ok(ApiResponse<List<object>>.SuccessResult(
                result, $"{result.Count} audit logs retrieved"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, ApiResponse<List<object>>.FailureResult(
                "An error occurred while retrieving audit logs"));
        }
    }

    private Guid? GetDoctorIdFromClaims()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}