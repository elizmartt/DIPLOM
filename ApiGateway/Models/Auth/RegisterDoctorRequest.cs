
using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models.Auth
{
    public class RegisterDoctorRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [MinLength(3, ErrorMessage = "Full name must be at least 3 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = string.Empty;

        public string Specialization { get; set; } = "Ընդհանուր";

        public string HospitalAffiliation { get; set; } = "";
    }
}