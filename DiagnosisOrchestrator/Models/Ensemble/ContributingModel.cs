namespace DiagnosisOrchestrator.Models.Ensemble
{
    public class ContributingModel
    {
        public string Source { get; set; } = string.Empty;
        public string Prediction { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public double Weight { get; set; }
        public bool AgreesWithFinal { get; set; }
    }
}