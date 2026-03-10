using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Entities;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
namespace MedicalDiagnosticSystem.MedicalDiagnostic.Data.Services
{

    public class DatabaseIntegrationService
    {
        private readonly IDiagnosisCaseRepository _caseRepo;
        private readonly IPatientRepository _patientRepo;
        private readonly IDoctorRepository _doctorRepo;
        private readonly IMedicalImageRepository _imageRepo;
        private readonly IClinicalSymptomRepository _symptomRepo;
        private readonly ILabTestRepository _labTestRepo;
        private readonly IImagingResultRepository _imagingResultRepo;
        private readonly IClinicalResultRepository _clinicalResultRepo;
        private readonly ILaboratoryResultRepository _labResultRepo;
        private readonly IUnifiedDiagnosisResultRepository _unifiedResultRepo;
        private readonly IAuditLogRepository _auditRepo;
        private readonly ILogger<DatabaseIntegrationService> _logger;

        public DatabaseIntegrationService(
            IDiagnosisCaseRepository caseRepo,
            IPatientRepository patientRepo,
            IDoctorRepository doctorRepo,
            IMedicalImageRepository imageRepo,
            IClinicalSymptomRepository symptomRepo,
            ILabTestRepository labTestRepo,
            IImagingResultRepository imagingResultRepo,
            IClinicalResultRepository clinicalResultRepo,
            ILaboratoryResultRepository labResultRepo,
            IUnifiedDiagnosisResultRepository unifiedResultRepo,
            IAuditLogRepository auditRepo,
            ILogger<DatabaseIntegrationService> logger)
        {
            _caseRepo = caseRepo;
            _patientRepo = patientRepo;
            _doctorRepo = doctorRepo;
            _imageRepo = imageRepo;
            _symptomRepo = symptomRepo;
            _labTestRepo = labTestRepo;
            _imagingResultRepo = imagingResultRepo;
            _clinicalResultRepo = clinicalResultRepo;
            _labResultRepo = labResultRepo;
            _unifiedResultRepo = unifiedResultRepo;
            _auditRepo = auditRepo;
            _logger = logger;
        }


        private async Task SafeLogAuditAsync(
            string action,
            Guid? doctorId = null,
            Guid? caseId = null,
            string? entityType = null,
            Guid? entityId = null,
            Dictionary<string, object>? actionDetails = null)
        {
            try
            {
                await _auditRepo.LogActionAsync(
                    action: action,
                    doctorId: doctorId,
                    caseId: caseId,
                    entityType: entityType,
                    entityId: entityId,
                    actionDetails: actionDetails
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Audit log write failed for action={Action} caseId={CaseId}. " +
                    "Clinical data flow continues.",
                    action, caseId);
            }
        }

        #region Diagnosis Case Management

       
        public async Task<Guid> CreateDiagnosisCaseAsync(
            Guid doctorId,
            Guid patientId,
            string? diagnosisType = null,
            string? priority = null,
            Guid? caseId = null)
        {
            try
            {
                var diagnosisCase = new DiagnosisCase
                {
                    CaseId = caseId ?? Guid.NewGuid(),
                    DoctorId = doctorId,
                    PatientId = patientId,
                    DiagnosisType = diagnosisType,
                    Priority = priority,
                    Status = "pending"
                };

                await _caseRepo.CreateAsync(diagnosisCase);

                _logger.LogInformation(
                    "✓ Created diagnosis case {CaseId} for patient {PatientId} by doctor {DoctorId}",
                    diagnosisCase.CaseId, patientId, doctorId);

             /*   await SafeLogAuditAsync(
               //     action: "CREATE_DIAGNOSIS_CASE",
                 //   doctorId: doctorId,
                   // caseId: diagnosisCase.CaseId,
                    entityType: "DiagnosisCase",
                    entityId: diagnosisCase.CaseId
                );*/

                return diagnosisCase.CaseId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to create diagnosis case");
                throw;
            }
        }

        public async Task UpdateCaseStatusAsync(Guid caseId, string status)
        {
            try
            {
                await _caseRepo.UpdateStatusAsync(caseId, status);

                _logger.LogInformation("✓ Updated case {CaseId} status → {Status}", caseId, status);

                await SafeLogAuditAsync(
                    action: $"UPDATE_CASE_STATUS_{status.ToUpper()}",
                    caseId: caseId,
                    entityType: "DiagnosisCase",
                    entityId: caseId,
                    actionDetails: new Dictionary<string, object> { { "new_status", status } }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to update case {CaseId} status to {Status}", caseId, status);
                throw;
            }
        }

        #endregion

        #region Input Data Storage

        public async Task<Guid> SaveMedicalImageAsync(
            Guid caseId,
            string imageType,
            string scanArea,
            string filePath,
            long? fileSizeBytes = null,
            Dictionary<string, object>? dicomMetadata = null)
        {
            try
            {
                var image = new MedicalImage
                {
                    CaseId = caseId,
                    ImageType = imageType,
                    ScanArea = scanArea,
                    FilePath = filePath,
                    //PreprocessingSteps = "[]",
                    FileSizeBytes = fileSizeBytes,
                    DicomMetadata = dicomMetadata != null ? JsonSerializer.SerializeToDocument(dicomMetadata) : null

                };

                await _imageRepo.CreateAsync(image);

                _logger.LogInformation(
                    "✓ Saved medical image {ImageId} for case {CaseId}: {ImageType} / {ScanArea}",
                    image.ImageId, caseId, imageType, scanArea);

                await SafeLogAuditAsync(
                    action: "UPLOAD_MEDICAL_IMAGE",
                    caseId: caseId,
                    entityType: "MedicalImage",
                    entityId: image.ImageId,
                    actionDetails: new Dictionary<string, object>
                    {
                        { "image_type", imageType },
                        { "scan_area", scanArea }
                    }
                );

                return image.ImageId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to save medical image for case {CaseId}", caseId);
                throw;
            }
        }

        public async Task<Guid> SaveClinicalSymptomsAsync(
            Guid caseId,
            Dictionary<string, bool> symptoms,
            string? bloodPressure = null,
            int? heartRate = null,
            decimal? temperature = null,
            bool? smokingHistory = null,
            Dictionary<string, bool>? familyHistory = null)
        {
            try
            {
                var clinicalSymptom = new ClinicalSymptom
                {
                    CaseId = caseId,
                    Symptoms = symptoms != null ? JsonSerializer.Serialize(symptoms) : null,
                    BloodPressure = bloodPressure,
                    HeartRate = heartRate,
                    Temperature = temperature.HasValue ? (double?)temperature.Value : null,
                    SmokingHistory = smokingHistory,
                    FamilyHistory = symptoms != null ? JsonSerializer.Serialize(familyHistory) : null,

                };

                await _symptomRepo.CreateAsync(clinicalSymptom);

                _logger.LogInformation(
                    "✓ Saved clinical symptoms {SymptomId} for case {CaseId}",
                    clinicalSymptom.SymptomId, caseId);

                await SafeLogAuditAsync(
                    action: "SAVE_CLINICAL_SYMPTOMS",
                    caseId: caseId,
                    entityType: "ClinicalSymptom",
                    entityId: clinicalSymptom.SymptomId
                );

                return clinicalSymptom.SymptomId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to save symptoms for case {CaseId}", caseId);
                throw;
            }
        }

        public async Task<Guid> SaveLabTestAsync(
            Guid caseId,
            DateTime testDate,
            Dictionary<string, double> testResults,
            string? labName = null,
            Dictionary<string, string>? referenceRanges = null)
        {
            try
            {
                var labTest = new LabTest
                {
                    CaseId = caseId,
                    TestDate = testDate,
                    TestResults = testResults != null ? JsonSerializer.Serialize(testResults) : null,
                    LabName = labName,
                    ReferenceRanges = referenceRanges != null ? JsonSerializer.Serialize(referenceRanges) : null,
                };

                await _labTestRepo.CreateAsync(labTest);

                _logger.LogInformation(
                    "✓ Saved lab test {LabId} for case {CaseId}",
                    labTest.LabId, caseId);

                await SafeLogAuditAsync(
                    action: "SAVE_LAB_TEST",
                    caseId: caseId,
                    entityType: "LabTest",
                    entityId: labTest.LabId
                );

                return labTest.LabId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to save lab test for case {CaseId}", caseId);
                throw;
            }
        }

        #endregion

        #region AI Module Results
    
        public async Task<int> SaveImagingResultAsync(
            Guid caseId,
            string prediction,
            double confidence,                                          // double — matches DB
            Dictionary<string, double>? probabilities = null,
            double? processingTimeMs = null,                            // double — matches DB
            Dictionary<string, object>? explainabilityData = null,
            bool success = true,
            string? errorMessage = null)
        {
            try
            {
                var result = new ImagingResult
                {
                    DiagnosisCaseId = caseId,
                    Prediction = prediction,
                    Confidence = confidence,
                    Probabilities = probabilities != null ? JsonSerializer.Serialize(probabilities) : null,
                    ExplainabilityData = explainabilityData != null ? JsonSerializer.Serialize(explainabilityData) : null,
                    ProcessingTimeMs = processingTimeMs,
                    Success = success,
                    ErrorMessage = errorMessage
                };

                await _imagingResultRepo.CreateAsync(result);

                _logger.LogInformation(
                    "✓ Saved imaging result id={Id} for case {CaseId}: {Prediction} ({Confidence:P2})",
                    result.Id, caseId, prediction, confidence);

                await SafeLogAuditAsync(
                    action: "SAVE_IMAGING_RESULT",
                    caseId: caseId,
                    entityType: "ImagingResult",
                    entityId: null,                                     
                    actionDetails: new Dictionary<string, object>
                    {
                        { "prediction", prediction },
                        { "confidence", confidence }
                    }
                );

                return result.Id;                                       
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to save imaging result for case {CaseId}", caseId);
                throw;
            }
        }

        
        public async Task<int> SaveClinicalResultAsync(
            Guid caseId,
            string prediction,
            double confidence,                                          
            Dictionary<string, double>? probabilities = null,
            double? processingTimeMs = null,                            
            Dictionary<string, object>? explainabilityData = null,
            bool success = true,
            string? errorMessage = null)
        {
            try
            {
                var result = new ClinicalResult
                {
                    DiagnosisCaseId = caseId,
                    Prediction = prediction,
                    Confidence = confidence,
                    Probabilities = probabilities != null ? JsonSerializer.Serialize(probabilities) : null,
                    ExplainabilityData = explainabilityData != null ? JsonSerializer.Serialize(explainabilityData) : null,
                    ProcessingTimeMs = processingTimeMs,
                    Success = success,
                    ErrorMessage = errorMessage
                };

                await _clinicalResultRepo.CreateAsync(result);

                _logger.LogInformation(
                    " Saved clinical result id={Id} for case {CaseId}: {Prediction} ({Confidence:P2})",
                    result.Id, caseId, prediction, confidence);

                await SafeLogAuditAsync(
                    action: "SAVE_CLINICAL_RESULT",
                    caseId: caseId,
                    entityType: "ClinicalResult",
                    entityId: null,
                    actionDetails: new Dictionary<string, object>
                    {
                        { "prediction", prediction },
                        { "confidence", confidence }
                    }
                );

                return result.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to save clinical result for case {CaseId}", caseId);
                throw;
            }
        }

        
        public async Task<int> SaveLaboratoryResultAsync(
            Guid caseId,
            string prediction,
            double confidence,                                          
            Dictionary<string, double>? probabilities = null,
            double? processingTimeMs = null,                            
            Dictionary<string, object>? explainabilityData = null,
            bool success = true,
            string? errorMessage = null)
        {
            try
            {
                var result = new LaboratoryResult
                {
                    DiagnosisCaseId = caseId,
                    Prediction = prediction,
                    Confidence = confidence,
                    Probabilities = probabilities != null ? JsonSerializer.Serialize(probabilities) : null,
                    ExplainabilityData = explainabilityData != null ? JsonSerializer.Serialize(explainabilityData) : null,                    ProcessingTimeMs = processingTimeMs,
                    Success = success,
                    ErrorMessage = errorMessage
                };

                await _labResultRepo.CreateAsync(result);

                _logger.LogInformation(
                    "✓ Saved laboratory result id={Id} for case {CaseId}: {Prediction} ({Confidence:P2})",
                    result.Id, caseId, prediction, confidence);

                await SafeLogAuditAsync(
                    action: "SAVE_LABORATORY_RESULT",
                    caseId: caseId,
                    entityType: "LaboratoryResult",
                    entityId: null,
                    actionDetails: new Dictionary<string, object>
                    {
                        { "prediction", prediction },
                        { "confidence", confidence }
                    }
                );

                return result.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to save laboratory result for case {CaseId}", caseId);
                throw;
            }
        }

        #endregion

        #region Unified Results

      
        public async Task<int> SaveUnifiedDiagnosisResultAsync(
            Guid caseId,
            string finalDiagnosis,
            double overallConfidence,                                   
            Dictionary<string, double>? ensembleProbabilities = null,
            List<string>? contributingModules = null,
            string? riskLevel = null,
            List<string>? recommendations = null,
            Dictionary<string, object>? explainabilitySummary = null,
            double? totalProcessingTimeMs = null,                       
            string status = "completed",
            string? errorDetails = null)
        {
            try
            {
                var result = new UnifiedDiagnosisResult
                {
                    DiagnosisCaseId = caseId,
                    FinalDiagnosis = finalDiagnosis,
                    OverallConfidence = overallConfidence,
                    EnsembleProbabilities = ensembleProbabilities != null ? JsonSerializer.Serialize(ensembleProbabilities) : null,
                    ContributingModules = contributingModules?.ToArray(),  
                    RiskLevel = riskLevel,
                    Recommendations = recommendations?.ToArray(),          
                    ExplainabilitySummary = explainabilitySummary != null 
                        ? JsonSerializer.Serialize(explainabilitySummary) 
                        : "{}",
                    TotalProcessingTimeMs = totalProcessingTimeMs.HasValue ? (int?)Math.Round(totalProcessingTimeMs.Value) : null,
                    Status = status,
                    ErrorDetails = errorDetails
                };

                await _unifiedResultRepo.CreateAsync(result);

                _logger.LogInformation(
                    "✓ Saved unified result id={Id} for case {CaseId}: {Diagnosis} ({Confidence:P2})",
                    result.Id, caseId, finalDiagnosis, overallConfidence);

                await SafeLogAuditAsync(
                    action: "SAVE_UNIFIED_RESULT",
                    caseId: caseId,
                    entityType: "UnifiedDiagnosisResult",
                    entityId: null,
                    actionDetails: new Dictionary<string, object>
                    {
                        { "final_diagnosis", finalDiagnosis },
                        { "overall_confidence", overallConfidence },
                        { "risk_level", riskLevel ?? "unknown" }
                    }
                );

                return result.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "✗ Failed to save unified result for case {CaseId}", caseId);
                throw;
            }
        }

        #endregion

        #region Complete Workflow

        public async Task<CompleteDiagnosisData?> GetCompleteDiagnosisAsync(Guid caseId)
        {
            try
            {
                var diagnosisCase = await _caseRepo.GetByIdAsync(caseId);
                if (diagnosisCase == null)
                    return null;

                var images = await _imageRepo.GetByCaseIdAsync(caseId);
                var symptoms = await _symptomRepo.GetByCaseIdAsync(caseId);
                var labTests = await _labTestRepo.GetByCaseIdAsync(caseId);
                var imagingResult = await _imagingResultRepo.GetByCaseIdAsync(caseId);
                var clinicalResult = await _clinicalResultRepo.GetByCaseIdAsync(caseId);
                var labResult = await _labResultRepo.GetByCaseIdAsync(caseId);
                var unifiedResult = await _unifiedResultRepo.GetByCaseIdAsync(caseId);
                var auditLogs = await _auditRepo.GetByCaseIdAsync(caseId);

                return new CompleteDiagnosisData
                {
                    DiagnosisCase = diagnosisCase,
                    MedicalImages = images.ToList(),
                    ClinicalSymptoms = symptoms,
                    LabTests = labTests.ToList(),
                    ImagingResult = imagingResult,
                    ClinicalResult = clinicalResult,
                    LaboratoryResult = labResult,
                    UnifiedResult = unifiedResult,
                    AuditTrail = auditLogs.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to retrieve complete diagnosis for case {CaseId}", caseId);
                return null;
            }
        }

        public async Task<bool> AreAllModulesCompleteAsync(Guid caseId)
        {
            var imagingResult = await _imagingResultRepo.GetByCaseIdAsync(caseId);
            var clinicalResult = await _clinicalResultRepo.GetByCaseIdAsync(caseId);
            var labResult = await _labResultRepo.GetByCaseIdAsync(caseId);

            return imagingResult != null && clinicalResult != null && labResult != null;
        }

        #endregion

        #region Patient & Doctor Management

        public async Task<Guid> GetOrCreatePatientAsync(
            string patientCode,
            int? age = null,
            string? gender = null)
        {
            
            var existingPatient = await _patientRepo.GetByPatientCodeAsync(patientCode);
            if (existingPatient != null)
                return existingPatient.PatientId;

            
            var patient = new Patient
            {
                PatientId = Guid.NewGuid(),
                PatientCode = patientCode,
                Age = age ?? 0,  
                Gender = gender ?? "Unknown",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _patientRepo.CreateAsync(patient);
            return result.PatientId;
        }

        #endregion
    }

    #region DTOs

    public class CompleteDiagnosisData
    {
        public DiagnosisCase DiagnosisCase { get; set; } = null!;
        public List<MedicalImage> MedicalImages { get; set; } = new();
        public ClinicalSymptom? ClinicalSymptoms { get; set; }
        public List<LabTest> LabTests { get; set; } = new();
        public ImagingResult? ImagingResult { get; set; }
        public ClinicalResult? ClinicalResult { get; set; }
        public LaboratoryResult? LaboratoryResult { get; set; }
        public UnifiedDiagnosisResult? UnifiedResult { get; set; }
        public List<AuditLog> AuditTrail { get; set; } = new();
    }

    #endregion
}