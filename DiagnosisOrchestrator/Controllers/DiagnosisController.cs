using DiagnosisOrchestrator.Models;
using DiagnosisOrchestrator.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosisOrchestrator.Controllers
{
    /// <summary>
    /// API Controller for diagnosis orchestration
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DiagnosisController : ControllerBase
    {
        private readonly IDiagnosisOrchestratorService _orchestrator;
        private readonly ILogger<DiagnosisController> _logger;

        public DiagnosisController(
            IDiagnosisOrchestratorService orchestrator,
            ILogger<DiagnosisController> logger)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Submit a diagnosis request for synchronous processing
        /// </summary>
        /// <param name="request">Diagnosis request with patient data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Unified diagnosis result</returns>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(UnifiedDiagnosis), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UnifiedDiagnosis>> ProcessDiagnosisSync(
            [FromBody] DiagnosisRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request cannot be null");
                }

                _logger.LogInformation(
                    "Received sync diagnosis request for case {CaseId}",
                    request.DiagnosisCaseId);

                var result = await _orchestrator.OrchestrateAsync(request, cancellationToken);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sync diagnosis request");
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Submit a diagnosis request for asynchronous processing via Kafka
        /// </summary>
        /// <param name="request">Diagnosis request</param>
        /// <returns>Request ID for tracking</returns>
        [HttpPost("async")]
        [ProducesResponseType(typeof(DiagnosisRequestResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DiagnosisRequestResponse>> ProcessDiagnosisAsync(
            [FromBody] DiagnosisRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request cannot be null");
                }

                _logger.LogInformation(
                    "Received async diagnosis request for case {CaseId}",
                    request.DiagnosisCaseId);

                var requestId = await _orchestrator.QueueDiagnosisAsync(request);

                var response = new DiagnosisRequestResponse
                {
                    RequestId = requestId,
                    Status = "Queued",
                    Message = "Diagnosis request queued for processing",
                    StatusUrl = Url.Action(nameof(GetDiagnosisStatus), new { id = requestId })
                };

                return AcceptedAtAction(
                    nameof(GetDiagnosisStatus),
                    new { id = requestId },
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queueing async diagnosis request");
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get the status and result of a diagnosis request
        /// </summary>
        /// <param name="id">Diagnosis case ID</param>
        /// <returns>Unified diagnosis result if available</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UnifiedDiagnosis), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UnifiedDiagnosis>> GetDiagnosisStatus(Guid id)
        {
            try
            {
                _logger.LogInformation("Retrieving diagnosis status for case {CaseId}", id);

                var result = await _orchestrator.GetDiagnosisStatusAsync(id);

                if (result == null)
                {
                    return NotFound(new
                    {
                        error = "Not found",
                        message = $"No diagnosis found for case ID: {id}"
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving diagnosis status for case {CaseId}", id);
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
        public ActionResult<HealthResponse> HealthCheck()
        {
            return Ok(new HealthResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "Diagnosis Orchestrator"
            });
        }
    }

    /// <summary>
    /// Response for async diagnosis request
    /// </summary>
    public record DiagnosisRequestResponse
    {
        public required Guid RequestId { get; init; }
        public required string Status { get; init; }
        public required string Message { get; init; }
        public string? StatusUrl { get; init; }
    }

    /// <summary>
    /// Health check response
    /// </summary>
    public record HealthResponse
    {
        public required string Status { get; init; }
        public required DateTime Timestamp { get; init; }
        public required string Service { get; init; }
    }
}
