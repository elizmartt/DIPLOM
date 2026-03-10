using System.Text.Json;
using DiagnosisOrchestrator.Models;
using DiagnosisOrchestrator.Models.Ensemble;
using DiagnosisOrchestrator.Services;
using Microsoft.AspNetCore.Mvc;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Services;
using MedicalDiagnostic.Data.Services;
using Npgsql;
using DatabaseIntegrationService = MedicalDiagnostic.Data.Services.DatabaseIntegrationService;

namespace DiagnosisOrchestrator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosisController : ControllerBase
    {
        private readonly KafkaProducerService _producer;
        private readonly KafkaConsumerService _consumer;
        private readonly DatabaseIntegrationService _dbService;
        private readonly ILogger<DiagnosisController> _logger;
        private readonly IConfiguration _configuration;

        public DiagnosisController(
            KafkaProducerService producer,
            KafkaConsumerService consumer,
            DatabaseIntegrationService dbService,
            ILogger<DiagnosisController> logger,
            IConfiguration configuration)
        {
            _producer = producer;
            _consumer = consumer;
            _dbService = dbService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("brain/sync")]
        public async Task<ActionResult<UnifiedDiagnosis>> ProcessBrainDiagnosisSync(
            [FromBody] DiagnosisRequest request,
            CancellationToken ct)
        {
            return await ProcessDiagnosisAsync(request, "Brain",
                _producer.PublishBrainDiagnosisRequestAsync, ct);
        }

        [HttpPost("lung/sync")]
        public async Task<ActionResult<UnifiedDiagnosis>> ProcessLungDiagnosisSync(
            [FromBody] DiagnosisRequest request,
            CancellationToken ct)
        {
            return await ProcessDiagnosisAsync(request, "Lung",
                _producer.PublishLungDiagnosisRequestAsync, ct);
        }

        private static string MapConfidenceToPriority(double confidence, bool isReliable)
        {
            if (!isReliable) return "high"; // unreliable result = needs urgent doctor attention

            return confidence switch
            {
                >= 0.80 => "low", // high confidence = AI is sure, lower urgency
                >= 0.50 => "normal", // medium confidence
                _ => "high" // low confidence = doctor needs to review urgently
            };
        }

        private async Task SafeUpdatePriority(Guid caseId, string priority)
        {
            try
            {
                await _dbService.UpdateCasePriorityAsync(caseId, priority);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not update priority for case {CaseId}", caseId);
            }
        }

        private async Task<ActionResult<UnifiedDiagnosis>> ProcessDiagnosisAsync(
            DiagnosisRequest request,
            string diagnosisType,
            Func<DiagnosisRequest, Task> publishAction,
            CancellationToken ct)
        {
            Guid caseId = request.DiagnosisCaseId;

            try
            {
                var requestId = caseId.ToString();
                _logger.LogInformation("Received {Type} diagnosis request {RequestId}", diagnosisType, requestId);

                // Count how many modules will actually send a response
                var expectedModules = 0;
                if (request.ImagingData    != null) expectedModules++;
                if (request.ClinicalData   != null) expectedModules++;
                if (request.LaboratoryData != null) expectedModules++;
                if (expectedModules == 0) expectedModules = 1; // safety minimum

                _consumer.RegisterRequest(requestId, timeoutSeconds: 60, expectedCount: expectedModules);

                await SafeUpdateStatus(caseId, "processing");

                await publishAction(request);
                _logger.LogInformation("Published {Type} request to Kafka", diagnosisType);

                // var predictions = await _consumer.WaitForResponsesAsync(requestId, ct);

                //  var ensembleResult = await _consumer.ProcessEnsembleAsync(requestId, predictions);
                var predictions = await _consumer.WaitForResponsesAsync(requestId, ct);
                var ensembleResult = await _consumer.ProcessEnsembleAsync(requestId, predictions);
                _logger.LogInformation("Received {Count}/3 predictions, running ensemble...", predictions.Count);

                var priority = MapConfidenceToPriority(ensembleResult.FinalConfidence, ensembleResult.IsReliable);
                await SafeUpdatePriority(caseId, priority);

                var finalStatus = ensembleResult.IsReliable ? "completed" : "completed_with_warnings";
                await SafeUpdateStatus(caseId, finalStatus);

                var result = new UnifiedDiagnosis
                {
                    DiagnosisCaseId = caseId,
                    FinalDiagnosis = MapEnsembleDiagnosisToType(ensembleResult.FinalDiagnosis),
                    OverallConfidence = ensembleResult.FinalConfidence,
                    ModulePredictions = predictions,
                    Status = OrchestrationStatus.Completed,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation(
                    "{Type} completed: {Diagnosis} (Confidence: {Confidence:P1}, Reliable: {Reliable})",
                    diagnosisType, ensembleResult.FinalDiagnosis,
                    ensembleResult.FinalConfidence, ensembleResult.IsReliable);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {Type} diagnosis", diagnosisType);
                await SafeUpdateStatus(caseId, "failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task SaveInputDataToDatabase(DiagnosisRequest request, string diagnosisType)
        {
            var caseId = request.DiagnosisCaseId;

            try
            {
                if (request.ImagingData != null && request.ImagingData.Count > 0)
                {
                    string scanArea = diagnosisType == "Lung" ? "Chest" : "Brain";
                    string imageType = diagnosisType == "Lung" ? "CT" : "MRI";

                    await _dbService.SaveMedicalImageAsync(
                        caseId: caseId,
                        imageType: imageType,
                        scanArea: scanArea,
                        filePath: "base64_inline",
                        fileSizeBytes: null,
                        dicomMetadata: request.ImagingData
                    );
                    _logger.LogInformation("Saved medical image for case {CaseId}", caseId);
                }

                if (request.ClinicalData != null && request.ClinicalData.Count > 0)
                {
                    var symptoms = new Dictionary<string, bool>();
                    foreach (var kvp in request.ClinicalData)
                    {
                        symptoms[kvp.Key] = kvp.Value switch
                        {
                            bool b => b,
                            int i => i > 0,
                            long l => l > 0,
                            double d => d > 0,
                            float f => f > 0,
                            string s => bool.TryParse(s, out var parsed) && parsed,
                            _ => kvp.Value != null
                        };
                    }

                    await _dbService.SaveClinicalSymptomsAsync(caseId: caseId, symptoms: symptoms);
                    _logger.LogInformation("Saved clinical symptoms for case {CaseId}", caseId);
                }

                if (request.LaboratoryData != null && request.LaboratoryData.Count > 0)
                {
                    var testResults = new Dictionary<string, double>();
                    foreach (var kvp in request.LaboratoryData)
                    {
                        if (double.TryParse(kvp.Value?.ToString(), out var val))
                            testResults[kvp.Key] = val;
                    }

                    await _dbService.SaveLabTestAsync(
                        caseId: caseId,
                        testDate: DateTime.UtcNow,
                        testResults: testResults
                    );
                    _logger.LogInformation("Saved lab results for case {CaseId}", caseId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save input data — continuing anyway");
            }
        }

        [HttpGet("{caseId}/explanations")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetShapExplanations(Guid caseId)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                const string sql = @"
                    SELECT 
                        model_name,
                        predicted_class,
                        base_value,
                        total_positive_impact,
                        total_negative_impact,
                        explanation_data,
                        created_at
                    FROM shap_explanations
                    WHERE diagnosis_case_id = @CaseId
                    ORDER BY created_at
                ";

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@CaseId", caseId);

                var explanations = new List<object>();

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var explanationJson = reader.GetString(5);
                    var explanation = JsonDocument.Parse(explanationJson).RootElement;

                    explanations.Add(new
                    {
                        ModelName = reader.GetString(0),
                        PredictedClass = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                        BaseValue = reader.IsDBNull(2) ? (decimal?)null : reader.GetDecimal(2),
                        TotalPositiveImpact = reader.IsDBNull(3) ? (decimal?)null : reader.GetDecimal(3),
                        TotalNegativeImpact = reader.IsDBNull(4) ? (decimal?)null : reader.GetDecimal(4),
                        Summary = explanation.TryGetProperty("summary", out var summary) ? summary.GetString() : null,
                        TopPositiveFeatures = explanation.TryGetProperty("top_positive_features", out var topPos)
                            ? topPos
                            : (JsonElement?)null,
                        TopNegativeFeatures = explanation.TryGetProperty("top_negative_features", out var topNeg)
                            ? topNeg
                            : (JsonElement?)null,
                        FeatureImpacts = explanation.TryGetProperty("feature_impacts", out var impacts)
                            ? impacts
                            : (JsonElement?)null,
                        CreatedAt = reader.GetDateTime(6)
                    });
                }

                if (!explanations.Any())
                    return NotFound(new { Message = $"No SHAP explanations found for diagnosis case {caseId}" });

                return Ok(new
                {
                    DiagnosisCaseId = caseId,
                    ExplanationCount = explanations.Count,
                    Explanations = explanations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SHAP explanations for case {CaseId}", caseId);
                return StatusCode(500, new { Message = "Error retrieving SHAP explanations", Error = ex.Message });
            }
        }

        private static DiagnosisType MapEnsembleDiagnosisToType(string diagnosis)
        {
            if (string.IsNullOrEmpty(diagnosis))
                return DiagnosisType.Inconclusive;

            var lower = diagnosis.ToLower();

            if (lower.Contains("glioma") ||
                lower.Contains("lung_cancer") ||
                lower.Contains("malignant") ||
                lower.Contains("abnormal"))
                return DiagnosisType.Malignant;

            if (lower.Contains("meningioma") ||
                lower.Contains("pituitary") ||
                lower.Contains("no_cancer") ||
                lower.Contains("normal") ||
                lower.Contains("benign"))
                return DiagnosisType.Benign;

            return DiagnosisType.Inconclusive;
        }

        private async Task SafeUpdateStatus(Guid caseId, string status)
        {
            try
            {
                await _dbService.UpdateCaseStatusAsync(caseId, status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not update case {CaseId} status to {Status}", caseId, status);
            }
        }

        [HttpGet("case/{caseId}")]
        public async Task<ActionResult> GetDiagnosisCase(Guid caseId)
        {
            try
            {
                var diagnosis = await _dbService.GetCompleteDiagnosisAsync(caseId);

                if (diagnosis == null)
                    return NotFound(new { error = $"Case {caseId} not found" });

                var response = new Dictionary<string, object?>
                {
                    ["case_id"] = diagnosis.DiagnosisCase.CaseId,
                    ["status"] = diagnosis.DiagnosisCase.Status,
                    ["created_at"] = diagnosis.DiagnosisCase.CreatedAt,
                    ["completed_at"] = diagnosis.DiagnosisCase.CompletedAt,
                    ["images_count"] = diagnosis.MedicalImages.Count,
                    ["has_symptoms"] = diagnosis.ClinicalSymptoms != null,
                    ["has_lab_results"] = diagnosis.LabTests.Count > 0
                };

                if (diagnosis.ImagingResult != null)
                    response["imaging_result"] = new
                    {
                        prediction = diagnosis.ImagingResult.Prediction,
                        confidence = diagnosis.ImagingResult.Confidence
                    };

                if (diagnosis.ClinicalResult != null)
                    response["clinical_result"] = new
                    {
                        prediction = diagnosis.ClinicalResult.Prediction,
                        confidence = diagnosis.ClinicalResult.Confidence
                    };

                if (diagnosis.LaboratoryResult != null)
                    response["laboratory_result"] = new
                    {
                        prediction = diagnosis.LaboratoryResult.Prediction,
                        confidence = diagnosis.LaboratoryResult.Confidence
                    };

                if (diagnosis.UnifiedResult != null)
                    response["unified_result"] = new
                    {
                        final_diagnosis = diagnosis.UnifiedResult.FinalDiagnosis,
                        overall_confidence = diagnosis.UnifiedResult.OverallConfidence,
                        risk_level = diagnosis.UnifiedResult.RiskLevel,
                        recommendations = diagnosis.UnifiedResult.Recommendations
                    };

                response["audit_logs_count"] = diagnosis.AuditTrail.Count;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving case {CaseId}", caseId);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("image-analysis")]
        public async Task<IActionResult> TriggerImageAnalysis([FromBody] ImageAnalysisRequest request)
        {
            try
            {
                _logger.LogInformation(
                    "Received image analysis request for image: {ImageId}, case: {CaseId}",
                    request.ImageId, request.DiagnosisCaseId);

                var message = new
                {
                    case_id = request.DiagnosisCaseId.ToString(),
                    image_id = request.ImageId.ToString(),
                    image_path = request.ImagePath,
                    image_type = request.ImageType,
                    scan_area = request.ScanArea,
                    timestamp = DateTime.UtcNow.ToString("O")
                };

                await _producer.PublishToTopicAsync("image-analysis", message);

                return Ok(new
                {
                    success = true,
                    message = "Image analysis request queued successfully",
                    imageId = request.ImageId,
                    caseId = request.DiagnosisCaseId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering image analysis for image: {ImageId}", request.ImageId);
                return StatusCode(500, new { error = "Failed to trigger image analysis", details = ex.Message });
            }
        }

        public class ImageAnalysisRequest
        {
            public Guid DiagnosisCaseId { get; set; }
            public Guid ImageId { get; set; }
            public string ImagePath { get; set; }
            public string ImageType { get; set; }
            public string ScanArea { get; set; }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "Healthy",
                service = "Diagnosis Orchestrator",
                database = "Connected",
                endpoints = new[] { "/api/diagnosis/brain/sync", "/api/diagnosis/lung/sync" }
            });
        }
    }
}
   
    