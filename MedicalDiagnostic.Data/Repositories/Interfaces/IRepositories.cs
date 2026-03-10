using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Entities;

namespace MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces
{
  
    public interface IDiagnosisCaseRepository
    {
        Task<DiagnosisCase> CreateAsync(DiagnosisCase diagnosisCase);
        Task<DiagnosisCase?> GetByIdAsync(Guid caseId);
        Task<DiagnosisCase> UpdateAsync(DiagnosisCase diagnosisCase);
        Task<DiagnosisCase> UpdateStatusAsync(Guid caseId, string status);
        Task<IEnumerable<DiagnosisCase>> GetByDoctorIdAsync(Guid doctorId, int limit = 50);
        Task<IEnumerable<DiagnosisCase>> GetByPatientIdAsync(Guid patientId, int limit = 50);
        Task<IEnumerable<DiagnosisCase>> GetRecentCasesAsync(int limit = 20);
        Task<IEnumerable<DiagnosisCase>> GetByStatusAsync(string status, int limit = 50);
        Task<IEnumerable<DiagnosisCase>> GetByDoctorIdAndStatusAsync(Guid doctorId, string status, int limit = 50);
        Task DeleteAsync(Guid caseId);
    }

    
    public interface IPatientRepository
    {
        Task<Patient> CreateAsync(Patient patient);
        Task<Patient?> GetByIdAsync(Guid patientId);
        Task<Patient?> GetByPatientCodeAsync(string patientCode);
        Task<Patient> UpdateAsync(Patient patient);
        Task<IEnumerable<Patient>> GetAllAsync(int limit = 100);
    }

    
    public interface IDoctorRepository
    {
        Task<Doctor> CreateAsync(Doctor doctor);
        Task<Doctor?> GetByIdAsync(Guid doctorId);
        Task<Doctor?> GetByEmailAsync(string email);
        Task<Doctor> UpdateAsync(Doctor doctor);
        Task<IEnumerable<Doctor>> GetActiveAsync(int limit = 100);
        Task<bool> DeactivateAsync(Guid doctorId);
        Task<IEnumerable<Doctor>> GetAllAsync(int limit = 100);

    }

    
    public interface IMedicalImageRepository
    {
        Task<MedicalImage> CreateAsync(MedicalImage image);
        Task<MedicalImage?> GetByIdAsync(Guid imageId);
        Task<IEnumerable<MedicalImage>> GetByCaseIdAsync(Guid caseId);
        Task<MedicalImage> UpdateAsync(MedicalImage image);
        Task<bool> MarkAsPreprocessedAsync(Guid imageId, List<string> preprocessingSteps);
    }

    
    public interface IClinicalSymptomRepository
    {
        Task<ClinicalSymptom> CreateAsync(ClinicalSymptom symptom);
        Task<ClinicalSymptom?> GetByIdAsync(Guid symptomId);
        Task<ClinicalSymptom?> GetByCaseIdAsync(Guid caseId);
        Task<ClinicalSymptom> UpdateAsync(ClinicalSymptom symptom);
    }

    
    public interface ILabTestRepository
    {
        Task<LabTest> CreateAsync(LabTest labTest);
        Task<LabTest?> GetByIdAsync(Guid labId);
        Task<IEnumerable<LabTest>> GetByCaseIdAsync(Guid caseId);
        Task<LabTest> UpdateAsync(LabTest labTest);
    }

    
    public interface IImagingResultRepository
    {
        Task<ImagingResult> CreateAsync(ImagingResult result);
        Task<ImagingResult?> GetByIdAsync(int id);
        Task<ImagingResult?> GetByCaseIdAsync(Guid caseId);
        Task<IEnumerable<ImagingResult>> GetRecentAsync(int limit = 50);
    }

   
    public interface IClinicalResultRepository
    {
        Task<ClinicalResult> CreateAsync(ClinicalResult result);
        Task<ClinicalResult?> GetByIdAsync(int id);
        Task<ClinicalResult?> GetByCaseIdAsync(Guid caseId);
        Task<IEnumerable<ClinicalResult>> GetRecentAsync(int limit = 50);
    }

    
    public interface ILaboratoryResultRepository
    {
        Task<LaboratoryResult> CreateAsync(LaboratoryResult result);
        Task<LaboratoryResult?> GetByIdAsync(int id);
        Task<LaboratoryResult?> GetByCaseIdAsync(Guid caseId);
        Task<IEnumerable<LaboratoryResult>> GetRecentAsync(int limit = 50);
    }

    
    public interface IUnifiedDiagnosisResultRepository
    {
        Task<UnifiedDiagnosisResult> CreateAsync(UnifiedDiagnosisResult result);
        Task<UnifiedDiagnosisResult?> GetByIdAsync(int id);
        Task<UnifiedDiagnosisResult?> GetByCaseIdAsync(Guid caseId);
        Task<UnifiedDiagnosisResult> UpdateAsync(UnifiedDiagnosisResult result);
        Task<IEnumerable<UnifiedDiagnosisResult>> GetRecentAsync(int limit = 50);
        Task<IEnumerable<UnifiedDiagnosisResult>> GetByDiagnosisAsync(string diagnosis, int limit = 50);
        Task<IEnumerable<UnifiedDiagnosisResult>> GetByRiskLevelAsync(string riskLevel, int limit = 50);
    }

    
    public interface IAuditLogRepository
    {
        Task<AuditLog> CreateAsync(AuditLog log);
        Task LogActionAsync(
            string action,
            Guid? doctorId = null,
            Guid? caseId = null,
            string? entityType = null,
            Guid? entityId = null,
            Dictionary<string, object>? actionDetails = null,
            string? ipAddress = null,
            string? userAgent = null
        );
        Task<IEnumerable<AuditLog>> GetByCaseIdAsync(Guid caseId);
        Task<IEnumerable<AuditLog>> GetByDoctorIdAsync(Guid doctorId, int limit = 100);
        Task<IEnumerable<AuditLog>> GetRecentAsync(int limit = 100);
        Task<IEnumerable<AuditLog>> GetByActionAsync(string action, int limit = 100);
    }
}