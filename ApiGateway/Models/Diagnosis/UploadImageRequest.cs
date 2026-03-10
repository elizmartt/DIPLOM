using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models.Diagnosis
{
    public class UploadImageRequest
    {
        [Required(ErrorMessage = "Image type is required")]
        public string ImageType { get; set; } = string.Empty; 

        [Required(ErrorMessage = "Scan area is required")]
        public string ScanArea { get; set; } = string.Empty; 

        [Required(ErrorMessage = "Image data is required")]
        public string ImageData { get; set; } = string.Empty;
        public Dictionary<string, object>? DicomMetadata { get; set; }
    }
}