using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models.Diagnosis
{
    public class CreateDiagnosisCaseRequest
    {
        [Required(ErrorMessage = "Patient ID is required")]
        public Guid PatientId { get; set; }

        [Required(ErrorMessage = "Diagnosis type is required")]
        public string DiagnosisType { get; set; } = string.Empty; // "brain_tumor", "lung_cancer"
        
        public string? DoctorNotes { get; set; }
    }
}