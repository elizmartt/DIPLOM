namespace DiagnosisOrchestrator.Models.Ensemble
{
    public class AgreementMetrics
    {
        public double AgreementScore { get; set; }
        public double WeightedAgreement { get; set; }
        public double ConsensusStrength { get; set; }
        public int AgreeingModels { get; set; }
        public int TotalModels { get; set; }
    }
}