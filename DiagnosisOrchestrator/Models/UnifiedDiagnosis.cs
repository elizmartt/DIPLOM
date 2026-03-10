
namespace DiagnosisOrchestrator.Models
{
    public class UnifiedDiagnosis
    {
        public Guid DiagnosisCaseId { get; set; }
        public DiagnosisType FinalDiagnosis { get; set; }
        public double OverallConfidence { get; set; }
        public List<ModulePrediction> ModulePredictions { get; set; } = new();
        public Dictionary<string, double> EnsembleProbabilities { get; set; } = new();
        public List<string> ContributingModules { get; set; } = new();
        public RiskLevel RiskLevel { get; set; }
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, object> ExplainabilitySummary { get; set; } = new();
        public OrchestrationStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
        public double? TotalProcessingTimeMs { get; set; }
        public string? ErrorDetails { get; set; }
    }

    public class ModulePrediction
    {
        public string ModuleName { get; set; } = string.Empty;
        public DiagnosisType Prediction { get; set; }
        public double Confidence { get; set; }
        public Dictionary<string, double> Probabilities { get; set; } = new();
        public Dictionary<string, object> ExplainabilityData { get; set; } = new();
        public double ProcessingTimeMs { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum DiagnosisType
    {
        Benign,
        Malignant,
        Inconclusive
    }

    public enum RiskLevel
    {
        Low,
        Moderate,
        High,
        Critical
    }

    public enum OrchestrationStatus
    {
        Pending,
        Processing,
        Completed,
        PartialSuccess,
        Failed
    }
}