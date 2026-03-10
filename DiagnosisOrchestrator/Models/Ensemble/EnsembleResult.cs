

namespace DiagnosisOrchestrator.Models.Ensemble
{
    public class EnsembleResult
    {
        public string FinalDiagnosis { get; set; } = string.Empty;
        public double FinalConfidence { get; set; }
        public double WeightedScore { get; set; }
        public double AgreementScore { get; set; }
        public double ConfidenceVariance { get; set; }
        public string ModelAvailability { get; set; } = string.Empty;
        public DegradationLevel DegradationLevel { get; set; }
        public bool IsReliable { get; set; }
        public List<ContributingModel> ContributingModels { get; set; } = new();
        public List<AlternativeDiagnosis> AlternativeDiagnoses { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}