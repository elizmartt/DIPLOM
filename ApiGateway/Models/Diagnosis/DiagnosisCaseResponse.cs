namespace ApiGateway.Models.Diagnosis
{
    public class DiagnosisCaseResponse
    {
        public Guid CaseId { get; set; }
        public Guid PatientId { get; set; }
        public Guid DoctorId { get; set; }
        public string DiagnosisType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string? DoctorDiagnosis { get; set; }
        public string? DoctorNotes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? PatientName { get; set; }
        
       
        public string? PatientCode { get; set; }
        public int? PatientAge { get; set; }
        public string? DoctorName { get; set; }
    }
}