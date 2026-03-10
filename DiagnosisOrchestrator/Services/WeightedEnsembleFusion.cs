using System;
using System.Collections.Generic;
using System.Linq;
using DiagnosisOrchestrator.Models.Ensemble;

namespace DiagnosisOrchestrator.Services
{

    public class WeightedEnsembleFusion
    {
        // Model weights 
        private const double IMAGING_WEIGHT = 0.60;
        private const double LABS_WEIGHT = 0.20;
        private const double SYMPTOMS_WEIGHT = 0.20;
        
        // Confidence thresholds
        private const double MIN_INDIVIDUAL_CONFIDENCE = 0.50;
        private const double HIGH_CONFIDENCE_THRESHOLD = 0.85;
        private const double AGREEMENT_THRESHOLD = 0.75;
        
        public EnsembleResult FusePredictions(
            ModelPrediction? imagingPrediction,
            ModelPrediction? labsPrediction,
            ModelPrediction? symptomsPrediction)
        {
            var availableModels = new List<(ModelPrediction prediction, double weight, string source)>();
            
            if (imagingPrediction != null)
                availableModels.Add((imagingPrediction, IMAGING_WEIGHT, "Imaging"));
            if (labsPrediction != null)
                availableModels.Add((labsPrediction, LABS_WEIGHT, "Labs"));
            if (symptomsPrediction != null)
                availableModels.Add((symptomsPrediction, SYMPTOMS_WEIGHT, "Symptoms"));
            
            int modelCount = availableModels.Count;
            
            if (modelCount == 0)
            {
                return new EnsembleResult
                {
                    FinalDiagnosis = "INSUFFICIENT_DATA",
                    FinalConfidence = 0.0,
                    ModelAvailability = "0/3",
                    DegradationLevel = DegradationLevel.Critical,
                    ErrorMessage = "No AI models available for diagnosis",
                    IsReliable = false
                };
            }
            
            double totalWeight = availableModels.Sum(m => m.weight);
            var normalizedModels = availableModels
                .Select(m => (m.prediction, normalizedWeight: m.weight / totalWeight, m.source))
                .ToList();
            
            
            var diagnosisVotes = new Dictionary<string, double>();
            var diagnosisConfidences = new Dictionary<string, List<double>>();
            
            foreach (var (prediction, weight, source) in normalizedModels)
            {
                string diagnosis = prediction.PredictedDisease;
                double confidence = prediction.Confidence;
                
                if (!diagnosisVotes.ContainsKey(diagnosis))
                {
                    diagnosisVotes[diagnosis] = 0.0;
                    diagnosisConfidences[diagnosis] = new List<double>();
                }
                
                diagnosisVotes[diagnosis] += weight * confidence;
                diagnosisConfidences[diagnosis].Add(confidence);
            }
            
            var winningDiagnosis = diagnosisVotes.OrderByDescending(kv => kv.Value).First();
            string finalDiagnosis = winningDiagnosis.Key;
            double weightedScore = winningDiagnosis.Value;
            
            var agreementMetrics = CalculateAgreementMetrics(
                normalizedModels,
                finalDiagnosis,
                diagnosisConfidences[finalDiagnosis]
            );
            
            double confidenceVariance = CalculateConfidenceVariance(
                diagnosisConfidences[finalDiagnosis]
            );
            
            double finalConfidence = AdjustFinalConfidence(
                weightedScore,
                agreementMetrics.AgreementScore,
                confidenceVariance,
                modelCount
            );
            
            DegradationLevel degradation = DetermineDegradationLevel(modelCount);
            
            bool isReliable = AssessReliability(
                finalConfidence,
                agreementMetrics.AgreementScore,
                confidenceVariance,
                modelCount
            );
            
            var contributingModels = normalizedModels
                .Select(m => new ContributingModel
                {
                    Source = m.source,
                    Prediction = m.prediction.PredictedDisease,
                    Confidence = m.prediction.Confidence,
                    Weight = m.normalizedWeight,
                    AgreesWithFinal = m.prediction.PredictedDisease == finalDiagnosis
                })
                .ToList();
            
            return new EnsembleResult
            {
                FinalDiagnosis = finalDiagnosis,
                FinalConfidence = finalConfidence,
                WeightedScore = weightedScore,
                AgreementScore = agreementMetrics.AgreementScore,
                ConfidenceVariance = confidenceVariance,
                ModelAvailability = $"{modelCount}/3",
                DegradationLevel = degradation,
                IsReliable = isReliable,
                ContributingModels = contributingModels,
                AlternativeDiagnoses = GetAlternativeDiagnoses(diagnosisVotes, finalDiagnosis),
                Explanation = GenerateExplanation(
                    finalDiagnosis,
                    finalConfidence,
                    agreementMetrics,
                    modelCount,
                    contributingModels
                )
            };
        }
        
        private AgreementMetrics CalculateAgreementMetrics(
            List<(ModelPrediction prediction, double weight, string source)> models,
            string finalDiagnosis,
            List<double> confidences)
        {
            int totalModels = models.Count;
            int agreeingModels = models.Count(m => m.prediction.PredictedDisease == finalDiagnosis);
            
            double agreementScore = (double)agreeingModels / totalModels;
            double weightedAgreement = models
                .Where(m => m.prediction.PredictedDisease == finalDiagnosis)
                .Sum(m => m.weight);
            double consensusStrength = confidences.Any() 
                ? confidences.Average() 
                : 0.0;
            
            return new AgreementMetrics
            {
                AgreementScore = agreementScore,
                WeightedAgreement = weightedAgreement,
                ConsensusStrength = consensusStrength,
                AgreeingModels = agreeingModels,
                TotalModels = totalModels
            };
        }
        
        
        private double CalculateConfidenceVariance(List<double> confidences)
        {
            if (confidences.Count <= 1)
                return 0.0;
            
            double mean = confidences.Average();
            double sumSquaredDiff = confidences.Sum(c => Math.Pow(c - mean, 2));
            double variance = sumSquaredDiff / confidences.Count;
            
            return variance;
        }
        
       
        private double AdjustFinalConfidence(
            double weightedScore,
            double agreementScore,
            double confidenceVariance,
            int modelCount)
        {
            double adjustedConfidence = weightedScore;
            
            if (agreementScore >= AGREEMENT_THRESHOLD && modelCount >= 2)
            {
                adjustedConfidence *= (1.0 + 0.1 * agreementScore);
            }
            
            if (confidenceVariance > 0.05)
            {
                adjustedConfidence *= (1.0 - Math.Min(confidenceVariance, 0.2));
            }
            
            if (modelCount < 3)
            {
                double reductionFactor = modelCount == 2 ? 0.90 : 0.80;
                adjustedConfidence *= reductionFactor;
            }
            
            return Math.Max(0.0, Math.Min(1.0, adjustedConfidence));
        }
        
    
        private DegradationLevel DetermineDegradationLevel(int modelCount)
        {
            return modelCount switch
            {
                3 => DegradationLevel.None,
                2 => DegradationLevel.Moderate,
                1 => DegradationLevel.Severe,
                _ => DegradationLevel.Critical
            };
        }
        
     
        private bool AssessReliability(
            double finalConfidence,
            double agreementScore,
            double confidenceVariance,
            int modelCount)
        {
            bool highConfidence = finalConfidence >= HIGH_CONFIDENCE_THRESHOLD;
            bool goodAgreement = agreementScore >= AGREEMENT_THRESHOLD;
            bool lowVariance = confidenceVariance < 0.05;
            bool sufficientModels = modelCount >= 2;
            
            if (!sufficientModels || finalConfidence < MIN_INDIVIDUAL_CONFIDENCE)
                return false;
            
            if (highConfidence && goodAgreement && lowVariance)
                return true;
            
            int qualityIndicators = 0;
            if (highConfidence) qualityIndicators++;
            if (goodAgreement) qualityIndicators++;
            if (lowVariance) qualityIndicators++;
            
            return qualityIndicators >= 2;
        }
        
        
        private List<AlternativeDiagnosis> GetAlternativeDiagnoses(
            Dictionary<string, double> diagnosisVotes,
            string finalDiagnosis)
        {
            return diagnosisVotes
                .Where(kv => kv.Key != finalDiagnosis)
                .OrderByDescending(kv => kv.Value)
                .Take(3)
                .Select(kv => new AlternativeDiagnosis
                {
                    Diagnosis = kv.Key,
                    WeightedScore = kv.Value,
                    ConfidenceDifference = diagnosisVotes[finalDiagnosis] - kv.Value
                })
                .ToList();
        }
        
      
        private string GenerateExplanation(
            string diagnosis,
            double confidence,
            AgreementMetrics agreement,
            int modelCount,
            List<ContributingModel> models)
        {
            var explanation = new System.Text.StringBuilder();
            
            explanation.AppendLine($"Diagnosis: {diagnosis}");
            explanation.AppendLine($"Confidence: {confidence:P1} ({modelCount}/3 models available)");
            explanation.AppendLine();
            
            explanation.AppendLine($"Model Agreement: {agreement.AgreeingModels}/{agreement.TotalModels} models agree");
            explanation.AppendLine($"Agreement Score: {agreement.AgreementScore:P1}");
            explanation.AppendLine();
            
            explanation.AppendLine("Contributing Models:");
            foreach (var model in models.OrderByDescending(m => m.Weight))
            {
                string agreementMark = model.AgreesWithFinal ? "✓" : "✗";
                explanation.AppendLine(
                    $"  {agreementMark} {model.Source}: {model.Prediction} " +
                    $"(Confidence: {model.Confidence:P1}, Weight: {model.Weight:P0})"
                );
            }
            
            if (modelCount < 3)
            {
                explanation.AppendLine();
                explanation.AppendLine($" Warning: Operating with {modelCount}/3 models. " +
                    "Confidence may be reduced due to incomplete data.");
            }
            
            return explanation.ToString();
        }
    }
}