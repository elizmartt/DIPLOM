using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models.Diagnosis
{
    public class SubmitLabTestsRequest
    {
        [Required(ErrorMessage = "Test date is required")]
        public DateTime TestDate { get; set; }

        [Required(ErrorMessage = "Lab name is required")]
        public string LabName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Test results are required")]
        public Dictionary<string, object> TestResults { get; set; } = new();
        
        public Dictionary<string, object>? ReferenceRanges { get; set; }
    }
}