
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MedicalDiagnosticSystem.MedicalDiagnostic.Data.Entities
{
  
    [Table("diagnosis_cases")]
    public class DiagnosisCase
    {
        [Key]
        [Column("case_id")]
        public Guid CaseId { get; set; }

        [Column("patient_id")]
        public Guid PatientId { get; set; }

        [Column("doctor_id")]
        public Guid DoctorId { get; set; }

        [Column("diagnosis_type")]
        public string DiagnosisType { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("priority")]
        public string? Priority { get; set; }

        [Column("doctor_diagnosis")]
        public string? DoctorDiagnosis { get; set; }

        [Column("doctor_notes")]
        public string? DoctorNotes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }
    }

   
    [Table("patients")]
    public class Patient
    {
        [Key]
        [Column("patient_id")]
        public Guid PatientId { get; set; }

        [Column("patient_code")]
        public string PatientCode { get; set; }
        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;   

        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;
        [Column("age")]
        public int Age { get; set; }
        

        [Column("gender")]
        public string Gender { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

 
    [Table("doctors")]
    public class Doctor
    {
        [Key]
        [Column("doctor_id")]
        public Guid DoctorId { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("specialization")]
        public string Specialization { get; set; }

        [Column("hospital_affiliation")]
        public string HospitalAffiliation { get; set; }
        [Column("role")]
        public string Role { get; set; } = "Doctor";

        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }
        [Column("must_change_password")]
        public bool MustChangePassword { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    
    public class MedicalImage
    {
        public Guid ImageId { get; set; }
        public Guid CaseId { get; set; }
        
        public string ImageType { get; set; }  // "DICOM" or "Standard"
        public string ScanArea { get; set; }
        public string FilePath { get; set; }
        public long? FileSizeBytes { get; set; }
        
        // DICOM-specific fields 
        public string? Modality { get; set; }           
        public string? StudyUid { get; set; }
        public string? SeriesUid { get; set; }
        public string? InstanceUid { get; set; }
        public DateTime? StudyDate { get; set; }
        public string? SeriesDescription { get; set; }
        public int? SeriesNumber { get; set; }
        public int? InstanceNumber { get; set; }
        public string? PixelSpacing { get; set; }
        public decimal? SliceThickness { get; set; }
        public int? WindowCenter { get; set; }
        public int? WindowWidth { get; set; }
        public int? Rows { get; set; }
        public int? Columns { get; set; }
        
     
        public JsonDocument? DicomMetadata { get; set; }
        
        public bool IsPreprocessed { get; set; }
        public DateTime UploadedAt { get; set; }
        
        
        public DiagnosisCase DiagnosisCase { get; set; }
     //   public byte[]? GradCamOverlayImage { get; set; }
      //  public byte[]? GradCamHeatmapImage { get; set; }
    }
}

    // ClinicalSymptom
    [Table("clinical_symptoms")]
    public class ClinicalSymptom
    {
        [Key]
        [Column("symptom_id")]
        public Guid SymptomId { get; set; }

        [Column("case_id")]
        public Guid CaseId { get; set; }

        [Column("symptoms", TypeName = "jsonb")]
        public string Symptoms { get; set; }

        [Column("blood_pressure")]
        public string? BloodPressure { get; set; }

        [Column("heart_rate")]
        public int? HeartRate { get; set; }

        [Column("temperature")]
        public double? Temperature { get; set; }

        [Column("smoking_history")]
        public bool? SmokingHistory { get; set; }

        [Column("family_history", TypeName = "jsonb")]
        public string FamilyHistory { get; set; }

        [Column("recorded_at")]
        public DateTime RecordedAt { get; set; }
    }

    // LabTest
    [Table("lab_tests")]
    public class LabTest
    {
        [Key]
        [Column("lab_id")]
        public Guid LabId { get; set; }

        [Column("case_id")]
        public Guid CaseId { get; set; }

        [Column("test_date")]
        public DateTime TestDate { get; set; }

        [Column("lab_name")]
        public string LabName { get; set; }

        [Column("test_results", TypeName = "jsonb")]
        public string TestResults { get; set; }

        [Column("reference_ranges", TypeName = "jsonb")]
        public string ReferenceRanges { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; }
    }

    // AuditLog
    [Table("audit_log")]
    public class AuditLog
    {
        [Key]
        [Column("log_id")]
        public Guid LogId { get; set; }

        [Column("action")]
        public string Action { get; set; }

        [Column("entity_type")]
        public string? EntityType { get; set; }

        [Column("entity_id")]
        public Guid? EntityId { get; set; }

        [Column("case_id")]
        public Guid? CaseId { get; set; }

        [Column("doctor_id")]
        public Guid? DoctorId { get; set; }

        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("user_agent")]
        public string? UserAgent { get; set; }

        [Column("action_details", TypeName = "jsonb")]
        public string? ActionDetails { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    // Result Entities (AI Module Results)
    [Table("clinical_results")]
    public class ClinicalResult
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("diagnosis_case_id")]
        public Guid DiagnosisCaseId { get; set; }
        
        [Column("prediction")]
        public string Prediction { get; set; }
        
        [Column("confidence")]
        public double Confidence { get; set; }
        
        [Column("probabilities", TypeName = "jsonb")]
        public string Probabilities { get; set; }
        
        [Column("processing_time_ms")]
        public double? ProcessingTimeMs { get; set; }
        
        [Column("explainability_data", TypeName = "jsonb")]
        public string ExplainabilityData { get; set; }
        
        [Column("success")]
        public bool Success { get; set; }
        
        [Column("error_message")]
        public string? ErrorMessage { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    [Table("imaging_results")]
    public class ImagingResult
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("diagnosis_case_id")]
        public Guid DiagnosisCaseId { get; set; }
        
        [Column("prediction")]
        public string Prediction { get; set; }
        
        [Column("confidence")]
        public double Confidence { get; set; }
        
        [Column("probabilities", TypeName = "jsonb")]
        public string Probabilities { get; set; }
        
        [Column("processing_time_ms")]
        public double? ProcessingTimeMs { get; set; }
        
        [Column("explainability_data", TypeName = "jsonb")]
        public string ExplainabilityData { get; set; }
        
        [Column("grad_cam_overlay_image")]
        public byte[]? GradCamOverlayImage { get; set; }

        [Column("grad_cam_heatmap_image")]
        public byte[]? GradCamHeatmapImage { get; set; }

        [Column("grad_cam_image_generated_at")]
        public DateTime? GradCamImageGeneratedAt { get; set; }
        [Column("success")]
        public bool Success { get; set; }
        
        [Column("error_message")]
        public string? ErrorMessage { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    [Table("laboratory_results")]
    public class LaboratoryResult
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("diagnosis_case_id")]
        public Guid DiagnosisCaseId { get; set; }
        
        [Column("prediction")]
        public string Prediction { get; set; }
        
        [Column("confidence")]
        public double Confidence { get; set; }
        
        [Column("probabilities", TypeName = "jsonb")]
        public string Probabilities { get; set; }
        
        [Column("processing_time_ms")]
        public double? ProcessingTimeMs { get; set; }
        
        [Column("explainability_data", TypeName = "jsonb")]
        public string ExplainabilityData { get; set; }
        
        [Column("success")]
        public bool Success { get; set; }
        
        [Column("error_message")]
        public string? ErrorMessage { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    [Table("unified_diagnosis_results")]
    public class UnifiedDiagnosisResult
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("diagnosis_case_id")]
        public Guid DiagnosisCaseId { get; set; }
        
        [Column("final_diagnosis")]
        public string FinalDiagnosis { get; set; }
        
        [Column("overall_confidence")]
        public double OverallConfidence { get; set; }
        
        [Column("ensemble_probabilities", TypeName = "jsonb")]
        public string EnsembleProbabilities { get; set; }
        
        [Column("contributing_modules")]
        public string[] ContributingModules { get; set; }
        
        [Column("risk_level")]
        public string RiskLevel { get; set; }
        
        [Column("recommendations")]
        public string[] Recommendations { get; set; }
        
        [Column("explainability_summary", TypeName = "jsonb")]
        public string ExplainabilitySummary { get; set; }
        
        [Column("total_processing_time_ms")]
        public double? TotalProcessingTimeMs { get; set; }
        
        [Column("status")]
        public string Status { get; set; }
        
        [Column("error_details")]
        public string? ErrorDetails { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
