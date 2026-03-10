using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models.Diagnosis
{
    public class SubmitSymptomsRequest
    {
        [Required(ErrorMessage = "Symptoms are required")]
        public List<string> Symptoms { get; set; } = new();

        public string? BloodPressure { get; set; }
        public int? HeartRate { get; set; }
        public double? Temperature { get; set; }
        public bool? SmokingHistory { get; set; }
        public Dictionary<string, object>? FamilyHistory { get; set; }
    }
}