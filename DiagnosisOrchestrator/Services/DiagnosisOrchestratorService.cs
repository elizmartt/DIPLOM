using DiagnosisOrchestrator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosisOrchestrator.Services
{
    /// <summary>
    /// Main orchestrator service that combines outputs from all AI modules
    /// Implements weighted voting and ensemble logic
    /// </summary>
    public class DiagnosisOrchestratorService : IDiagnosisOrchestratorService
    {
        private readonly IModuleClientService _moduleClient;
        private readonly IDiagnosisRepository _repository;
        private readonly IKafkaProducerService _kafkaProducer;
        private readonly ILogger<DiagnosisOrchestratorService> _logger;
        private readonly OrchestratorOptions _options;
        private readonly ModuleWeights _normalizedWeights;

        public DiagnosisOrchestratorService(
            IModuleClientService moduleClient,
            IDiagnosisRepository repository,
            IKafkaProducerService kafkaProducer,
            IOptions<OrchestratorOptions> options,
            ILogger<DiagnosisOrchestratorService> logger)
        {
            _moduleClient = moduleClient ?? throw new ArgumentNullException(nameof(moduleClient));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _normalizedWeights = _options.ModuleWeights.Normalize();

            _logger.LogInformation(
                "Orchestrator initialized - Strategy: {Strategy}, Weights: I:{Imaging} C:{Clinical} L:{Laboratory}",
                _options.EnsembleStrategy, 
                _normalizedWeights.Imaging, 
                _normalizedWeights.Clinical, 
                _normalizedWeights.Laboratory);
        }

        public async Task<UnifiedDiagnosis> OrchestrateAsync(
            DiagnosisRequest request, 
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation(
                "Starting orchestration for case {CaseId} with {ModuleCount} modules",
                request.DiagnosisCaseId,
                CountAvailableModules(request));

            try
            {
                // Call all available AI modules (in parallel if enabled)
                var predictions = await CollectModulePredictionsAsync(request, cancellationToken);

                // Validate minimum modules requirement
                var successfulPredictions = predictions.Where(p => p.Success).ToList();
                if (successfulPredictions.Count < _options.MinModulesRequired)
                {
                    _logger.LogWarning(
                        "Insufficient modules: {Available}/{Required} for case {CaseId}",
                        successfulPredictions.Count,
                        _options.MinModulesRequired,
                        request.DiagnosisCaseId);

                    return CreateInconclusive(
                        request.DiagnosisCaseId,
                        predictions,
                        stopwatch.Elapsed.TotalMilliseconds,
                        $"Insufficient data: only {successfulPredictions.Count} of {_options.MinModulesRequired} required modules available");
                }

                // Apply ensemble strategy
                var (finalDiagnosis, confidence, probabilities) = ApplyEnsembleStrategy(successfulPredictions);

                // Calculate risk level
                var riskLevel = CalculateRiskLevel(finalDiagnosis, confidence, successfulPredictions);

                // Generate clinical recommendations
                var recommendations = GenerateRecommendations(finalDiagnosis, confidence, successfulPredictions);

                // Aggregate explainability data
                var explainability = AggregateExplainability(successfulPredictions);

                stopwatch.Stop();

                var unifiedDiagnosis = new UnifiedDiagnosis
                {
                    DiagnosisCaseId = request.DiagnosisCaseId,
                    FinalDiagnosis = finalDiagnosis,
                    OverallConfidence = confidence,
                    ModulePredictions = predictions,
                    EnsembleProbabilities = probabilities,
                    ContributingModules = successfulPredictions.Select(p => p.ModuleName).ToList(),
                    RiskLevel = riskLevel,
                    Recommendations = recommendations,
                    ExplainabilitySummary = explainability,
                    Status = OrchestrationStatus.Completed,
                    Timestamp = DateTime.UtcNow,
                    TotalProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };

                // Persist to database
                await _repository.SaveDiagnosisAsync(unifiedDiagnosis, cancellationToken);

                _logger.LogInformation(
                    "Orchestration completed for case {CaseId}: {Diagnosis} ({Confidence:P2}) in {Time}ms",
                    request.DiagnosisCaseId,
                    finalDiagnosis,
                    confidence,
                    stopwatch.Elapsed.TotalMilliseconds);

                return unifiedDiagnosis;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Orchestration failed for case {CaseId}", request.DiagnosisCaseId);

                var failedDiagnosis = new UnifiedDiagnosis
                {
                    DiagnosisCaseId = request.DiagnosisCaseId,
                    FinalDiagnosis = DiagnosisType.Inconclusive,
                    OverallConfidence = 0.0,
                    ModulePredictions = new List<ModulePrediction>(),
                    EnsembleProbabilities = new Dictionary<string, double>(),
                    ContributingModules = new List<string>(),
                    RiskLevel = RiskLevel.High,
                    Recommendations = new List<string> { "System error occurred. Please retry or consult manually." },
                    ExplainabilitySummary = new Dictionary<string, object>(),
                    Status = OrchestrationStatus.Failed,
                    Timestamp = DateTime.UtcNow,
                    TotalProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                    ErrorDetails = ex.Message
                };

                await _repository.SaveDiagnosisAsync(failedDiagnosis, cancellationToken);
                return failedDiagnosis;
            }
        }

        public async Task<Guid> QueueDiagnosisAsync(DiagnosisRequest request)
        {
            _logger.LogInformation("Queueing diagnosis request for case {CaseId}", request.DiagnosisCaseId);
            await _kafkaProducer.PublishDiagnosisRequestAsync(request);
            return request.DiagnosisCaseId;
        }

        public async Task<UnifiedDiagnosis?> GetDiagnosisStatusAsync(Guid diagnosisCaseId)
        {
            return await _repository.GetDiagnosisByIdAsync(diagnosisCaseId);
        }

        #region Private Helper Methods

        private async Task<List<ModulePrediction>> CollectModulePredictionsAsync(
            DiagnosisRequest request,
            CancellationToken cancellationToken)
        {
            var predictions = new List<ModulePrediction>();

            if (_options.EnableParallelProcessing)
            {
                // Call all modules in parallel
                var tasks = new List<Task<ModulePrediction?>>();

                if (request.ImagingData != null)
                    tasks.Add(_moduleClient.CallImagingModuleAsync(request.ImagingData, cancellationToken));

                if (request.ClinicalData != null)
                    tasks.Add(_moduleClient.CallClinicalModuleAsync(request.ClinicalData, cancellationToken));

                if (request.LaboratoryData != null)
                    tasks.Add(_moduleClient.CallLaboratoryModuleAsync(request.LaboratoryData, cancellationToken));

                var results = await Task.WhenAll(tasks);
                predictions.AddRange(results.Where(r => r != null)!);
            }
            else
            {
                // Call modules sequentially
                if (request.ImagingData != null)
                {
                    var result = await _moduleClient.CallImagingModuleAsync(request.ImagingData, cancellationToken);
                    if (result != null) predictions.Add(result);
                }

                if (request.ClinicalData != null)
                {
                    var result = await _moduleClient.CallClinicalModuleAsync(request.ClinicalData, cancellationToken);
                    if (result != null) predictions.Add(result);
                }

                if (request.LaboratoryData != null)
                {
                    var result = await _moduleClient.CallLaboratoryModuleAsync(request.LaboratoryData, cancellationToken);
                    if (result != null) predictions.Add(result);
                }
            }

            return predictions;
        }

        private (DiagnosisType diagnosis, double confidence, Dictionary<string, double> probabilities) 
            ApplyEnsembleStrategy(List<ModulePrediction> predictions)
        {
            return _options.EnsembleStrategy.ToLower() switch
            {
                "weightedvoting" => WeightedVoting(predictions),
                "confidenceweighted" => ConfidenceWeightedVoting(predictions),
                "majorityvoting" => MajorityVoting(predictions),
                _ => WeightedVoting(predictions)
            };
        }

        /// <summary>
        /// Weighted voting based on predefined module weights
        /// </summary>
        private (DiagnosisType, double, Dictionary<string, double>) WeightedVoting(List<ModulePrediction> predictions)
        {
            var weightedProbs = new Dictionary<string, double>
            {
                ["benign"] = 0.0,
                ["malignant"] = 0.0
            };

            double totalWeight = 0.0;

            foreach (var prediction in predictions)
            {
                var weight = GetModuleWeight(prediction.ModuleName);
                totalWeight += weight;

                foreach (var prob in prediction.Probabilities)
                {
                    if (weightedProbs.ContainsKey(prob.Key))
                    {
                        weightedProbs[prob.Key] += prob.Value * weight;
                    }
                }
            }

            // Normalize probabilities
            if (totalWeight > 0)
            {
                var normalized = weightedProbs.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value / totalWeight
                );

                var maxProb = normalized.MaxBy(kvp => kvp.Value);
                var diagnosis = maxProb.Key == "malignant" ? DiagnosisType.Malignant : DiagnosisType.Benign;
                var confidence = maxProb.Value;

                // Check confidence threshold
                if (confidence < _options.ConfidenceThreshold)
                {
                    diagnosis = DiagnosisType.Inconclusive;
                }

                _logger.LogDebug("Weighted voting: {Diagnosis} with confidence {Confidence:P2}", diagnosis, confidence);

                return (diagnosis, confidence, normalized);
            }

            return (DiagnosisType.Inconclusive, 0.0, weightedProbs);
        }

        /// <summary>
        /// Confidence-weighted voting - modules with higher confidence get more weight
        /// </summary>
        private (DiagnosisType, double, Dictionary<string, double>) ConfidenceWeightedVoting(List<ModulePrediction> predictions)
        {
            var weightedProbs = new Dictionary<string, double>
            {
                ["benign"] = 0.0,
                ["malignant"] = 0.0
            };

            double totalWeight = 0.0;

            foreach (var prediction in predictions)
            {
                var baseWeight = GetModuleWeight(prediction.ModuleName);
                var confidenceWeight = baseWeight * prediction.Confidence; // Scale by confidence
                totalWeight += confidenceWeight;

                foreach (var prob in prediction.Probabilities)
                {
                    if (weightedProbs.ContainsKey(prob.Key))
                    {
                        weightedProbs[prob.Key] += prob.Value * confidenceWeight;
                    }
                }
            }

            if (totalWeight > 0)
            {
                var normalized = weightedProbs.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value / totalWeight
                );

                var maxProb = normalized.MaxBy(kvp => kvp.Value);
                var diagnosis = maxProb.Key == "malignant" ? DiagnosisType.Malignant : DiagnosisType.Benign;
                var confidence = maxProb.Value;

                if (confidence < _options.ConfidenceThreshold)
                {
                    diagnosis = DiagnosisType.Inconclusive;
                }

                return (diagnosis, confidence, normalized);
            }

            return (DiagnosisType.Inconclusive, 0.0, weightedProbs);
        }

        /// <summary>
        /// Simple majority voting - each module gets equal weight
        /// </summary>
        private (DiagnosisType, double, Dictionary<string, double>) MajorityVoting(List<ModulePrediction> predictions)
        {
            var votes = new Dictionary<DiagnosisType, int>();
            
            foreach (var prediction in predictions)
            {
                if (!votes.ContainsKey(prediction.Prediction))
                    votes[prediction.Prediction] = 0;
                votes[prediction.Prediction]++;
            }

            var majorityVote = votes.MaxBy(kvp => kvp.Value);
            var confidence = (double)majorityVote.Value / predictions.Count;

            var probabilities = new Dictionary<string, double>
            {
                ["benign"] = votes.GetValueOrDefault(DiagnosisType.Benign, 0) / (double)predictions.Count,
                ["malignant"] = votes.GetValueOrDefault(DiagnosisType.Malignant, 0) / (double)predictions.Count
            };

            var diagnosis = confidence < _options.ConfidenceThreshold 
                ? DiagnosisType.Inconclusive 
                : majorityVote.Key;

            return (diagnosis, confidence, probabilities);
        }

        private double GetModuleWeight(string moduleName)
        {
            return moduleName.ToLower() switch
            {
                "imaging" => _normalizedWeights.Imaging,
                "clinical" => _normalizedWeights.Clinical,
                "laboratory" => _normalizedWeights.Laboratory,
                _ => 0.0
            };
        }

        private RiskLevel CalculateRiskLevel(
            DiagnosisType diagnosis, 
            double confidence, 
            List<ModulePrediction> predictions)
        {
            if (diagnosis == DiagnosisType.Inconclusive)
                return RiskLevel.Moderate;

            if (diagnosis == DiagnosisType.Malignant)
            {
                if (confidence >= 0.90)
                    return RiskLevel.Critical;
                if (confidence >= 0.75)
                    return RiskLevel.High;
                return RiskLevel.Moderate;
            }

            // Benign cases
            if (confidence >= 0.90)
                return RiskLevel.Low;
            if (confidence >= 0.70)
                return RiskLevel.Moderate;
            return RiskLevel.High; // Low confidence in benign diagnosis warrants caution
        }

        private List<string> GenerateRecommendations(
            DiagnosisType diagnosis,
            double confidence,
            List<ModulePrediction> predictions)
        {
            var recommendations = new List<string>();

            if (diagnosis == DiagnosisType.Malignant)
            {
                recommendations.Add("Immediate consultation with oncology specialist recommended");
                recommendations.Add("Consider biopsy for histological confirmation");
                recommendations.Add("Schedule comprehensive staging workup");
                
                if (confidence < 0.80)
                {
                    recommendations.Add("Low confidence - recommend additional imaging or tests");
                }
            }
            else if (diagnosis == DiagnosisType.Benign)
            {
                recommendations.Add("Continue routine monitoring protocol");
                
                if (confidence < 0.80)
                {
                    recommendations.Add("Consider follow-up examination in 3-6 months");
                    recommendations.Add("Low confidence - additional tests may be warranted");
                }
            }
            else // Inconclusive
            {
                recommendations.Add("Insufficient data for definitive diagnosis");
                recommendations.Add("Recommend additional diagnostic tests");
                recommendations.Add("Clinical correlation strongly advised");
                
                var failedModules = predictions.Where(p => !p.Success).Select(p => p.ModuleName).ToList();
                if (failedModules.Any())
                {
                    recommendations.Add($"Failed modules: {string.Join(", ", failedModules)} - retry recommended");
                }
            }

            // Check for disagreement between modules
            var distinctPredictions = predictions.Select(p => p.Prediction).Distinct().Count();
            if (distinctPredictions > 1)
            {
                recommendations.Add("⚠️ Module disagreement detected - manual review strongly recommended");
            }

            return recommendations;
        }

        private Dictionary<string, object> AggregateExplainability(List<ModulePrediction> predictions)
        {
            var aggregated = new Dictionary<string, object>();

            foreach (var prediction in predictions)
            {
                if (prediction.ExplainabilityData != null)
                {
                    aggregated[$"{prediction.ModuleName}_explainability"] = prediction.ExplainabilityData;
                }
            }

            // Add summary statistics
            aggregated["module_agreement"] = CalculateModuleAgreement(predictions);
            aggregated["confidence_distribution"] = predictions
                .ToDictionary(p => p.ModuleName, p => p.Confidence);

            return aggregated;
        }

        private double CalculateModuleAgreement(List<ModulePrediction> predictions)
        {
            if (predictions.Count < 2) return 1.0;

            var malignantCount = predictions.Count(p => p.Prediction == DiagnosisType.Malignant);
            var benignCount = predictions.Count(p => p.Prediction == DiagnosisType.Benign);

            var maxAgreement = Math.Max(malignantCount, benignCount);
            return (double)maxAgreement / predictions.Count;
        }

        private UnifiedDiagnosis CreateInconclusive(
            Guid diagnosisCaseId,
            List<ModulePrediction> predictions,
            double processingTimeMs,
            string reason)
        {
            return new UnifiedDiagnosis
            {
                DiagnosisCaseId = diagnosisCaseId,
                FinalDiagnosis = DiagnosisType.Inconclusive,
                OverallConfidence = 0.0,
                ModulePredictions = predictions,
                EnsembleProbabilities = new Dictionary<string, double>
                {
                    ["benign"] = 0.0,
                    ["malignant"] = 0.0
                },
                ContributingModules = predictions.Where(p => p.Success).Select(p => p.ModuleName).ToList(),
                RiskLevel = RiskLevel.Moderate,
                Recommendations = new List<string> 
                { 
                    reason,
                    "Additional diagnostic information required",
                    "Manual clinical review recommended"
                },
                ExplainabilitySummary = new Dictionary<string, object>
                {
                    ["insufficient_data"] = true,
                    ["reason"] = reason
                },
                Status = OrchestrationStatus.PartialSuccess,
                Timestamp = DateTime.UtcNow,
                TotalProcessingTimeMs = processingTimeMs
            };
        }

        private int CountAvailableModules(DiagnosisRequest request)
        {
            int count = 0;
            if (request.ImagingData != null) count++;
            if (request.ClinicalData != null) count++;
            if (request.LaboratoryData != null) count++;
            return count;
        }

        #endregion
    }
}
