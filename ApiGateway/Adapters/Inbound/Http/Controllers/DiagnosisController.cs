using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiGateway.Models;
using ApiGateway.Models.Diagnosis;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Entities;
using System.Text.Json;
using ApiGateway.Core.Services;
using ApiGateway.Services;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Services;

namespace ApiGateway.Adapters.Inbound.Http.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Doctor")]
    public class DiagnosisController : ControllerBase
    {
        private readonly IDiagnosisCaseRepository _caseRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IMedicalImageRepository _imageRepository;
        private readonly IClinicalSymptomRepository _symptomRepository;
        private readonly ILabTestRepository _labTestRepository;
        private readonly IUnifiedDiagnosisResultRepository _resultsRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<DiagnosisController> _logger;
        private readonly DiagnosisKafkaService _kafkaService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEncryptionService _encryption;



        public DiagnosisController(
            IDiagnosisCaseRepository caseRepository,
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IMedicalImageRepository imageRepository,
            IClinicalSymptomRepository symptomRepository,
            ILabTestRepository labTestRepository,
            IUnifiedDiagnosisResultRepository resultsRepository,
            IAuditLogRepository auditLogRepository,
            DiagnosisKafkaService kafkaService,
            IServiceScopeFactory scopeFactory,
            ILogger<DiagnosisController> logger,
            IEncryptionService encryption)
        {
            _caseRepository = caseRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _imageRepository = imageRepository;
            _symptomRepository = symptomRepository;
            _labTestRepository = labTestRepository;
            _resultsRepository = resultsRepository;
            _auditLogRepository = auditLogRepository;
            _kafkaService = kafkaService;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _encryption = encryption;
                
        }


        [HttpPost("cases")]
        public async Task<ActionResult<ApiResponse<DiagnosisCaseResponse>>> CreateCase(
            [FromBody] CreateDiagnosisCaseRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<DiagnosisCaseResponse>.FailureResult(
                    "Invalid request data", errors));
            }

            try
            {
                var doctorId = GetDoctorIdFromClaims();
                if (doctorId == null)
                {
                    return Unauthorized(ApiResponse<DiagnosisCaseResponse>.FailureResult(
                        "Invalid doctor credentials"));
                }

                var patient = await _patientRepository.GetByIdAsync(request.PatientId);
                if (patient == null)
                {
                    return NotFound(ApiResponse<DiagnosisCaseResponse>.FailureResult(
                        "Patient not found"));
                }

                var diagnosisCase = new DiagnosisCase
                {
                    CaseId = Guid.NewGuid(),
                    PatientId = request.PatientId,
                    DoctorId = doctorId.Value,
                    DiagnosisType = request.DiagnosisType,
                    Status = "pending",
                    DoctorNotes = request.DoctorNotes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdCase = await _caseRepository.CreateAsync(diagnosisCase);


                await _auditLogRepository.LogActionAsync(
                    action: "CREATE_DIAGNOSIS_CASE",
                    doctorId: doctorId,
                    caseId: createdCase.CaseId,
                    entityType: "DiagnosisCase",
                    entityId: createdCase.CaseId,
                    actionDetails: new Dictionary<string, object>
                    {
                        { "PatientId", request.PatientId },
                        { "DiagnosisType", request.DiagnosisType }
                    },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString()
                );

                _logger.LogInformation(
                    "Diagnosis case created: {CaseId} for Patient: {PatientId} by Doctor: {DoctorId}",
                    createdCase.CaseId, request.PatientId, doctorId);

                var response = await MapToDiagnosisCaseResponse(createdCase);
                return Ok(ApiResponse<DiagnosisCaseResponse>.SuccessResult(
                    response, "Diagnosis case created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating diagnosis case");
                return StatusCode(500, ApiResponse<DiagnosisCaseResponse>.FailureResult(
                    "An error occurred while creating the diagnosis case"));
            }
        }
      
        [HttpDelete("cases/{caseId:guid}")]
        public async Task<IActionResult> DeleteCase(Guid caseId)
        {
            try
            {
                var doctorId = GetDoctorIdFromClaims();

                var diagnosisCase = await _caseRepository.GetByIdAsync(caseId);
                if (diagnosisCase == null)
                    return NotFound(new { error = $"Case {caseId} not found" });

                if (diagnosisCase.DoctorId != doctorId)
                    return Forbid();

                if (diagnosisCase.Status != "pending" && diagnosisCase.Status != "data_collection")
                    return BadRequest(new { error = $"Cannot delete a case with status '{diagnosisCase.Status}'. Only pending or data_collection cases can be deleted." });

                await _caseRepository.DeleteAsync(caseId);

                await _auditLogRepository.LogActionAsync(
                    action: "DELETE_CASE",
                    doctorId: doctorId,
                    caseId: caseId,
                    entityType: "DiagnosisCase",
                    entityId: caseId,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString()
                );

                return Ok(new { caseId, message = "Case deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting case {CaseId}", caseId);
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpGet("cases/{caseId:guid}")]
        public async Task<ActionResult<ApiResponse<DiagnosisCaseResponse>>> GetCase(Guid caseId)
        {
            try
            {
                var diagnosisCase = await _caseRepository.GetByIdAsync(caseId);
                
                if (diagnosisCase == null)
                {
                    return NotFound(ApiResponse<DiagnosisCaseResponse>.FailureResult(
                        "Diagnosis case not found"));
                }

                
                var doctorId = GetDoctorIdFromClaims();
                if (diagnosisCase.DoctorId != doctorId)
                {
                    return Forbid();
                }

                var response = await MapToDiagnosisCaseResponse(diagnosisCase);
                return Ok(ApiResponse<DiagnosisCaseResponse>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving diagnosis case: {CaseId}", caseId);
                return StatusCode(500, ApiResponse<DiagnosisCaseResponse>.FailureResult(
                    "An error occurred while retrieving the diagnosis case"));
            }
        }

        
        [HttpGet("cases")]
        public async Task<ActionResult<ApiResponse<List<DiagnosisCaseResponse>>>> ListCases(
            [FromQuery] string? status = null,
            [FromQuery] int limit = 50)
        {
            try
            {
                var doctorId = GetDoctorIdFromClaims();
                if (doctorId == null)
                {
                    return Unauthorized(ApiResponse<List<DiagnosisCaseResponse>>.FailureResult(
                        "Invalid doctor credentials"));
                }

                if (limit > 100) limit = 100;

                IEnumerable<DiagnosisCase> cases;
                
                if (!string.IsNullOrEmpty(status))
                {
                    cases = await _caseRepository.GetByStatusAsync(status, limit);
                    cases = cases.Where(c => c.DoctorId == doctorId);
                }
                else
                {
                    cases = await _caseRepository.GetByDoctorIdAsync(doctorId.Value, limit);
                }

                var responses = new List<DiagnosisCaseResponse>();
                foreach (var c in cases)
                {
                    responses.Add(await MapToDiagnosisCaseResponse(c));
                }

                return Ok(ApiResponse<List<DiagnosisCaseResponse>>.SuccessResult(
                    responses, $"Retrieved {responses.Count} diagnosis cases"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing diagnosis cases");
                return StatusCode(500, ApiResponse<List<DiagnosisCaseResponse>>.FailureResult(
                    "An error occurred while retrieving diagnosis cases"));
            }
        }

        [HttpGet("cases/{caseId:guid}/images")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetCaseImages(Guid caseId)
        {
            try
            {
                var diagnosisCase = await _caseRepository.GetByIdAsync(caseId);
                if (diagnosisCase == null)
                    return NotFound(ApiResponse<List<object>>.FailureResult("Case not found"));

                var doctorId = GetDoctorIdFromClaims();
                if (diagnosisCase.DoctorId != doctorId)
                    return Forbid();

                var images = await _imageRepository.GetByCaseIdAsync(caseId);
                var result = images.Select(img => (object)new
                {
                    ImageId = img.ImageId,
                    FilePath = img.FilePath,
                    ScanArea = img.ScanArea,
                    ImageType = img.ImageType,
                    UploadedAt = img.UploadedAt
                }).ToList();

                return Ok(ApiResponse<List<object>>.SuccessResult(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving images for case: {CaseId}", caseId);
                return StatusCode(500, ApiResponse<List<object>>.FailureResult("Error retrieving images"));
            }
        }
[HttpPost("cases/{caseId:guid}/images")]
[Consumes("multipart/form-data")]
public async Task<ActionResult<ApiResponse<object>>> UploadImage(
    [FromRoute] Guid caseId,
    IFormFile file,
    [FromForm] string scanArea,
    [FromServices] DicomService dicomService,
    [FromServices] S3Service s3Service)
{
    if (file == null || file.Length == 0)
        return BadRequest(ApiResponse<object>.FailureResult("No file uploaded"));

    try
    {
        var diagnosisCase = await _caseRepository.GetByIdAsync(caseId);
        if (diagnosisCase == null)
            return NotFound(ApiResponse<object>.FailureResult("Diagnosis case not found"));

        var doctorId = GetDoctorIdFromClaims();
        if (diagnosisCase.DoctorId != doctorId)
            return Forbid();

        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        var isDicom = fileExtension == ".dcm";

        var allowedExtensions = new[] { ".dcm", ".jpg", ".jpeg", ".png" };
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(ApiResponse<object>.FailureResult(
                $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}"));
        }

        string s3Key;
        using (var stream = file.OpenReadStream())
        {
            s3Key = await s3Service.UploadImageAsync(
                stream,
                caseId.ToString(),
                file.FileName,
                file.ContentType ?? "application/octet-stream");
        }

        _logger.LogInformation("File uploaded to S3: {S3Key}", s3Key);

        var medicalImage = new MedicalImage
        {
            ImageId = Guid.NewGuid(),
            CaseId = caseId,
            ImageType = isDicom ? "DICOM" : "Standard",
            ScanArea = scanArea,
            FilePath = s3Key,  
            FileSizeBytes = file.Length,
            IsPreprocessed = false,
            UploadedAt = DateTime.UtcNow
        };

        if (isDicom)
        {
            try
            {
               
                var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.dcm");
                using (var stream = file.OpenReadStream())
                using (var fs = new FileStream(tempPath, FileMode.Create))
                {
                    await stream.CopyToAsync(fs);
                }

                var dicomMetadata = await dicomService.ParseDicomFileAsync(tempPath);
                System.IO.File.Delete(tempPath);

                medicalImage.Modality = dicomMetadata.Modality;
                medicalImage.StudyUid = dicomMetadata.StudyUid;
                medicalImage.SeriesUid = dicomMetadata.SeriesUid;
                medicalImage.InstanceUid = dicomMetadata.InstanceUid;
                medicalImage.StudyDate = dicomMetadata.StudyDate;
                medicalImage.SeriesDescription = dicomMetadata.SeriesDescription;
                medicalImage.SeriesNumber = dicomMetadata.SeriesNumber;
                medicalImage.InstanceNumber = dicomMetadata.InstanceNumber;
                medicalImage.PixelSpacing = dicomMetadata.PixelSpacing;
                medicalImage.SliceThickness = dicomMetadata.SliceThickness;
                medicalImage.WindowCenter = dicomMetadata.WindowCenter;
                medicalImage.WindowWidth = dicomMetadata.WindowWidth;
                medicalImage.Rows = dicomMetadata.Rows;
                medicalImage.Columns = dicomMetadata.Columns;
                medicalImage.DicomMetadata = JsonSerializer.SerializeToDocument(dicomMetadata.AdditionalData);

                _logger.LogInformation("DICOM metadata parsed for image {ImageId}", medicalImage.ImageId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse DICOM metadata, continuing with basic info");
            }
        }

        var createdImage = await _imageRepository.CreateAsync(medicalImage);
        await _caseRepository.UpdateStatusAsync(caseId, "data_collection");

        await _auditLogRepository.LogActionAsync(
            action: "UPLOAD_MEDICAL_IMAGE",
            doctorId: doctorId,
            caseId: caseId,
            entityType: "MedicalImage",
            entityId: createdImage.ImageId,
            actionDetails: new Dictionary<string, object>
            {
                { "ImageType", isDicom ? "DICOM" : "Standard" },
                { "ScanArea", scanArea },
                { "FileName", file.FileName },
                { "FileSize", file.Length },
                { "S3Key", s3Key }
            },
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString()
        );

        return Ok(ApiResponse<object>.SuccessResult(
            new
            {
                ImageId = createdImage.ImageId,
                FilePath = s3Key,
                ImageType = isDicom ? "DICOM" : "Standard",
                IsDicom = isDicom,
                Modality = medicalImage.Modality,
                FileSize = file.Length
            },
            $"{(isDicom ? "DICOM" : "Standard")} image uploaded successfully"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error uploading medical image for case: {CaseId}", caseId);
        return StatusCode(500, ApiResponse<object>.FailureResult(
            "An error occurred while uploading the medical image"));
    }
}
[HttpGet("cases/{caseId:guid}/gradcam/{filename}")]
public async Task<IActionResult> ServeGradCam(
    Guid caseId,
    string filename,
    [FromServices] S3Service s3Service,
    [FromServices] IHttpClientFactory httpClientFactory)
{
    if (string.IsNullOrEmpty(filename) || filename.Contains(".."))
        return BadRequest("Invalid filename");

    var doctorId = GetDoctorIdFromClaims();
    if (doctorId == null)
        return Unauthorized();

    var isGradCam = filename.StartsWith("heatmap_") || filename.StartsWith("overlay_");
    var s3Key = isGradCam
        ? $"gradcam/{caseId}/{filename}"
        : $"patients/{caseId}/{filename}";   
    try
    {
        var presignedUrl = s3Service.GetPresignedUrl(s3Key, expiryMinutes: 60);
        var httpClient = httpClientFactory.CreateClient();
        var imageBytes = await httpClient.GetByteArrayAsync(presignedUrl);
        return File(imageBytes, "image/png");
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "GradCAM not found: {S3Key}", s3Key);
        return NotFound();
    }
}
[HttpGet("cases/{caseId:guid}/file/{filename}")]
public async Task<IActionResult> ServeFile(
    Guid caseId,
    string filename,
    [FromServices] S3Service s3Service,
    [FromServices] IHttpClientFactory httpClientFactory)
{
    if (string.IsNullOrEmpty(filename) || filename.Contains(".."))
        return BadRequest("Invalid filename");

    var doctorId = GetDoctorIdFromClaims();
    if (doctorId == null)
        return Unauthorized();

    var s3Key = $"patients/{caseId}/{filename}";
    try
    {
        var presignedUrl = s3Service.GetPresignedUrl(s3Key, expiryMinutes: 60);
        var httpClient = httpClientFactory.CreateClient();
        var imageBytes = await httpClient.GetByteArrayAsync(presignedUrl);

        var ext = Path.GetExtension(filename).ToLower();
        var contentType = ext switch
        {
            ".png"  => "image/png",
            ".jpg"  => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".dcm"  => "application/dicom",
            _       => "application/octet-stream"
        };

        return File(imageBytes, contentType);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "File not found: {S3Key}", s3Key);
        return NotFound();
    }
}
        [HttpPost("cases/{caseId:guid}/symptoms")]
        public async Task<ActionResult<ApiResponse<object>>> SubmitSymptoms(
            Guid caseId,
            [FromBody] SubmitSymptomsRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<object>.FailureResult(
                    "Invalid request data", errors));
            }

            try
            {
                
                var diagnosisCase = await _caseRepository.GetByIdAsync(caseId);
                if (diagnosisCase == null)
                {
                    return NotFound(ApiResponse<object>.FailureResult("Diagnosis case not found"));
                }

                var doctorId = GetDoctorIdFromClaims();
                if (diagnosisCase.DoctorId != doctorId)
                {
                    return Forbid();
                }

                
                var symptom = new ClinicalSymptom
                {
                    SymptomId = Guid.NewGuid(),
                    CaseId = caseId,
                    Symptoms = JsonSerializer.Serialize(request.Symptoms),
                    BloodPressure = request.BloodPressure,
                    HeartRate = request.HeartRate,
                    Temperature = request.Temperature,
                    SmokingHistory = request.SmokingHistory,
                    FamilyHistory = request.FamilyHistory != null
                        ? JsonSerializer.Serialize(request.FamilyHistory)
                        : "{}",
                    RecordedAt = DateTime.UtcNow
                };

                var createdSymptom = await _symptomRepository.CreateAsync(symptom);

               
                await _caseRepository.UpdateStatusAsync(caseId, "data_collection");

               
                await _auditLogRepository.LogActionAsync(
                    action: "SUBMIT_SYMPTOMS",
                    doctorId: doctorId,
                    caseId: caseId,
                    entityType: "ClinicalSymptom",
                    entityId: createdSymptom.SymptomId,
                    actionDetails: new Dictionary<string, object>
                    {
                        { "SymptomCount", request.Symptoms.Count }
                    },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString()
                );

                _logger.LogInformation(
                    "Clinical symptoms submitted: {SymptomId} for Case: {CaseId}",
                    createdSymptom.SymptomId, caseId);

                return Ok(ApiResponse<object>.SuccessResult(
                    new { SymptomId = createdSymptom.SymptomId },
                    "Clinical symptoms submitted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting symptoms for case: {CaseId}", caseId);
                return StatusCode(500, ApiResponse<object>.FailureResult(
                    "An error occurred while submitting clinical symptoms"));
            }
        }

       
       
        [HttpPost("cases/{caseId:guid}/lab-tests")]
        public async Task<ActionResult<ApiResponse<object>>> SubmitLabTests(
            Guid caseId,
            [FromBody] SubmitLabTestsRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<object>.FailureResult(
                    "Invalid request data", errors));
            }

            try
            {
                
                var diagnosisCase = await _caseRepository.GetByIdAsync(caseId);
                if (diagnosisCase == null)
                {
                    return NotFound(ApiResponse<object>.FailureResult("Diagnosis case not found"));
                }

                var doctorId = GetDoctorIdFromClaims();
                if (diagnosisCase.DoctorId != doctorId)
                {
                    return Forbid();
                }

                
                var labTest = new LabTest
                {
                    LabId = Guid.NewGuid(),
                    CaseId = caseId,
                    TestDate = request.TestDate,
                    LabName = request.LabName,
                    TestResults = JsonSerializer.Serialize(request.TestResults),
                    ReferenceRanges = request.ReferenceRanges != null
                        ? JsonSerializer.Serialize(request.ReferenceRanges)
                        : "{}",
                    UploadedAt = DateTime.UtcNow
                };

                var createdLabTest = await _labTestRepository.CreateAsync(labTest);

                
                await _caseRepository.UpdateStatusAsync(caseId, "data_collection");

                
                await _auditLogRepository.LogActionAsync(
                    action: "SUBMIT_LAB_TESTS",
                    doctorId: doctorId,
                    caseId: caseId,
                    entityType: "LabTest",
                    entityId: createdLabTest.LabId,
                    actionDetails: new Dictionary<string, object>
                    {
                        { "LabName", request.LabName },
                        { "TestDate", request.TestDate }
                    },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString()
                );

                _logger.LogInformation(
                    "Lab tests submitted: {LabId} for Case: {CaseId}",
                    createdLabTest.LabId, caseId);

                return Ok(ApiResponse<object>.SuccessResult(
                    new { LabTestId = createdLabTest.LabId },
                    "Lab test results submitted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting lab tests for case: {CaseId}", caseId);
                return StatusCode(500, ApiResponse<object>.FailureResult(
                    "An error occurred while submitting lab test results"));
            }
        }


[HttpPost("cases/{caseId:guid}/analyze")]
public async Task<ActionResult<ApiResponse<object>>> TriggerAnalysis(
    Guid caseId,
    [FromBody] TriggerAnalysisRequest request)
{
    try
    {
        var diagnosisCase = await _caseRepository.GetByIdAsync(caseId);
        if (diagnosisCase == null)
            return NotFound(ApiResponse<object>.FailureResult("Diagnosis case not found"));

        var doctorId = GetDoctorIdFromClaims();
        if (diagnosisCase.DoctorId != doctorId)
            return Forbid();


        await _caseRepository.UpdateStatusAsync(caseId, "analyzing");

        await _auditLogRepository.LogActionAsync(
            action: "TRIGGER_AI_ANALYSIS",
            doctorId: doctorId,
            caseId: caseId,
            entityType: "DiagnosisCase",
            entityId: caseId,
            actionDetails: new Dictionary<string, object>
            {
                { "IncludeImaging", request.IncludeImaging },
                { "IncludeClinical", request.IncludeClinical },
                { "IncludeLaboratory", request.IncludeLaboratory }
            },
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString()
        );

        var caseIdCapture       = caseId;
        var patientIdCapture    = diagnosisCase.PatientId;
        var diagTypeCapture     = diagnosisCase.DiagnosisType;
        var inclImaging         = request.IncludeImaging;
        var inclClinical        = request.IncludeClinical;
        var inclLab             = request.IncludeLaboratory;
        var scopeFactory        = _scopeFactory;
        var logger              = _logger;


        _ = Task.Run(async () =>
        {
            using var scope     = scopeFactory.CreateScope();
            var kafkaService    = scope.ServiceProvider.GetRequiredService<DiagnosisKafkaService>();
            var caseRepo        = scope.ServiceProvider.GetRequiredService<IDiagnosisCaseRepository>();
            try
            {
                var success = await kafkaService.SendDiagnosisRequestAsync(
                    caseIdCapture, patientIdCapture, diagTypeCapture,
                    inclImaging, inclClinical, inclLab);

                if (!success)
                {
                    logger.LogError("Orchestrator returned failure for case {CaseId}", caseIdCapture);
                    await caseRepo.UpdateStatusAsync(caseIdCapture, "failed");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background analysis failed for case {CaseId}", caseIdCapture);
                await caseRepo.UpdateStatusAsync(caseIdCapture, "failed");
            }
        });

        _logger.LogInformation("AI analysis queued (fire-and-forget) for Case: {CaseId}", caseId);

        return Ok(ApiResponse<object>.SuccessResult(
            new { Status = "analyzing", Message = "Diagnosis request sent to AI system successfully" },
            "AI analysis triggered successfully"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error triggering analysis for case: {CaseId}", caseId);
        return StatusCode(500, ApiResponse<object>.FailureResult(
            "An error occurred while triggering AI analysis"));
    }
}

        [HttpGet("cases/{caseId:guid}/results")]
        public async Task<ActionResult<ApiResponse<object>>> GetResults(Guid caseId)
        {
            try
            {
                
                var diagnosisCase = await _caseRepository.GetByIdAsync(caseId);
                if (diagnosisCase == null)
                {
                    return NotFound(ApiResponse<object>.FailureResult("Diagnosis case not found"));
                }

                var doctorId = GetDoctorIdFromClaims();
                if (diagnosisCase.DoctorId != doctorId)
                {
                    return Forbid();
                }

                
                var result = await _resultsRepository.GetByCaseIdAsync(caseId);
                
                if (result == null)
                {
                    return NotFound(ApiResponse<object>.FailureResult(
                        "No diagnosis results found for this case"));
                }

                var response = new
                {
                    result.Id,
                    result.DiagnosisCaseId,
                    result.FinalDiagnosis,
                    result.OverallConfidence,
                    EnsembleProbabilities = JsonSerializer.Deserialize<Dictionary<string, object>>(result.EnsembleProbabilities),
                    result.ContributingModules,
                    result.RiskLevel,
                    result.Recommendations,
                    ExplainabilitySummary = JsonSerializer.Deserialize<Dictionary<string, object>>(result.ExplainabilitySummary),
                    result.TotalProcessingTimeMs,
                    result.Status,
                    result.CreatedAt,
                    result.UpdatedAt
                };

                return Ok(ApiResponse<object>.SuccessResult(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving results for case: {CaseId}", caseId);
                return StatusCode(500, ApiResponse<object>.FailureResult(
                    "An error occurred while retrieving diagnosis results"));
            }
        }

        
        private Guid? GetDoctorIdFromClaims()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var doctorId))
                return doctorId;
            return null;
        }

        private async Task<DiagnosisCaseResponse> MapToDiagnosisCaseResponse(DiagnosisCase diagnosisCase)
        {
            var patient = await _patientRepository.GetByIdAsync(diagnosisCase.PatientId);
            var doctor = await _doctorRepository.GetByIdAsync(diagnosisCase.DoctorId);

            return new DiagnosisCaseResponse
            {
                CaseId = diagnosisCase.CaseId,
                PatientId = diagnosisCase.PatientId,
                DoctorId = diagnosisCase.DoctorId,
                DiagnosisType = diagnosisCase.DiagnosisType,
                Status = diagnosisCase.Status,
                Priority = diagnosisCase.Priority,
                DoctorDiagnosis = diagnosisCase.DoctorDiagnosis,
                DoctorNotes = diagnosisCase.DoctorNotes,
                CreatedAt = diagnosisCase.CreatedAt,
                UpdatedAt = diagnosisCase.UpdatedAt,
                CompletedAt = diagnosisCase.CompletedAt,
                PatientCode = patient?.PatientCode,
                PatientAge = patient?.Age,
                DoctorName = doctor?.FullName,
                PatientName = patient != null
                    ? $"{SafeDecrypt(patient.FirstName)} {SafeDecrypt(patient.LastName)}".Trim()
                    : null            };
        }
        private string SafeDecrypt(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            try { return _encryption.Decrypt(value); }
            catch (FormatException) { return value; }
            catch (System.Security.Cryptography.CryptographicException) { return value; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Decrypt failed, returning as-is");
                return value;
            }
        }
    }
}