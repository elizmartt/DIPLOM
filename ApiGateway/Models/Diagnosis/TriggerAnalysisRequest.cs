namespace ApiGateway.Models.Diagnosis
{
    public class TriggerAnalysisRequest
    {
        public bool IncludeImaging { get; set; } = true;
        public bool IncludeClinical { get; set; } = true;
        public bool IncludeLaboratory { get; set; } = true;
    }
}