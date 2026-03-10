

namespace DiagnosisOrchestrator.Models.Ensemble
{
    public class ModelPrediction
    {
        public string PredictedDisease { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public Dictionary<string, double>? ProbabilityDistribution { get; set; }
        public DateTime Timestamp { get; set; }
    }
}