namespace DiagnosisOrchestrator.Models.Ensemble
{
    public class AlternativeDiagnosis
    {
        public string Diagnosis { get; set; } = string.Empty;
        public double WeightedScore { get; set; }
        public double ConfidenceDifference { get; set; }
    }
}