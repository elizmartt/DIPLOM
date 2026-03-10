

namespace DiagnosisOrchestrator.Models
{
    public class DiagnosisRequest
    {
        public Guid DiagnosisCaseId { get; set; }
        public Guid PatientId { get; set; }
        public Dictionary<string, object>? ImagingData { get; set; }
        public Dictionary<string, object>? ClinicalData { get; set; }
        public Dictionary<string, object>? LaboratoryData { get; set; }
    }

}
/*
    public class ImagingData
    {
        public string? ImageData { get; set; }  // Base64 encoded image
    }

    public class ClinicalData
    {
        public List<string>? Symptoms { get; set; }
    }

    public class LaboratoryData
    {
        public Dictionary<string, object>? BloodTests { get; set; }
    }
*/