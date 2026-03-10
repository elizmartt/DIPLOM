namespace ApiGateway.Models.Patient
{
    public class PatientResponse
    {
        public Guid PatientId { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;  
        public string LastName { get; set; } = string.Empty;   
        public string FullName => $"{FirstName} {LastName}";   
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
 
    }
}