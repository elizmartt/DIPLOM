using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models.Patient
{
    public class CreatePatientRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;
    
        [Required]
        public string LastName { get; set; } = string.Empty;
    

        [Required]
        [Range(0, 150)]
        public int Age { get; set; }
    
        [Required]
        public string Gender { get; set; } = string.Empty; 
    
    }
}