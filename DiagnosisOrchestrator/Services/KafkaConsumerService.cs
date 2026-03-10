using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using DiagnosisOrchestrator.Models;
using DiagnosisOrchestrator.Models.Ensemble;
using DatabaseIntegrationService = MedicalDiagnosticSystem.MedicalDiagnostic.Data.Services.DatabaseIntegrationService;

namespace DiagnosisOrchestrator.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerService> _logger;
        private IConsumer<string, string>? _consumer;
        private readonly WeightedEnsembleFusion _ensembleFusion;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        private const string BrainImagingResults = "brain-imaging-results";
        private const string BrainClinicalResults = "brain-clinical-results";
        private const string BrainLabResults = "brain-lab-results";
        private const string LungImagingResults = "lung-imaging-results";
        private const string LungClinicalResults = "lung-clinical-results";
        private const string LungLabResults = "lung-lab-results";

        private readonly ConcurrentDictionary<string, PendingRequest> _pendingRequests = new();

        public KafkaConsumerService(
            ILogger<KafkaConsumerService> logger,
            WeightedEnsembleFusion ensembleFusion,
            IServiceProvider serviceProvider,
                IConfiguration configuration)
        {
            _logger = logger;
            _ensembleFusion = ensembleFusion;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Kafka consumer service started (background)");

            _ = Task.Run(async () =>
            {
                await Task.Delay(3000, stoppingToken);

                try
                {
                    var config = new ConsumerConfig
                    {
                        BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                        GroupId = "diagnosis-orchestrator-group",
                        AutoOffsetReset = AutoOffsetReset.Latest,
                        EnableAutoCommit = true,
                        MaxPartitionFetchBytes = 10485760,
                        AllowAutoCreateTopics = true
                    };

                    _consumer = new ConsumerBuilder<string, string>(config).Build();

                    var topicsToSubscribe = new List<string>
                    {
                        BrainImagingResults, BrainClinicalResults, BrainLabResults,
                        LungImagingResults, LungClinicalResults, LungLabResults
                    };

                    _consumer.Subscribe(topicsToSubscribe);
                    _logger.LogInformation("Subscribed to {Count} result topics", topicsToSubscribe.Count);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var result = _consumer.Consume(TimeSpan.FromSeconds(1));
                            if (result != null && !result.IsPartitionEOF)
                                await ProcessMessageAsync(result);
                        }
                        catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
                        {
                            await Task.Delay(1000, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error consuming message");
                            await Task.Delay(1000, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "FATAL: Kafka consumer failed!");
                }
                finally
                {
                    _consumer?.Close();
                }
            }, stoppingToken);

            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(ConsumeResult<string, string> result)
        {
            var requestId = result.Message.Key;
            _logger.LogInformation("Received response for {RequestId} from {Topic}", requestId, result.Topic);

            try
            {
                var response = JsonSerializer.Deserialize<ModuleResponse>(result.Message.Value,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (response == null) return;

                var prediction = new ModulePrediction
                {
                    ModuleName = GetModuleName(result.Topic),
                    Prediction = ParseDiagnosis(response.Prediction, result.Topic),
                    Confidence = response.Confidence,
                    Probabilities = response.Probabilities ?? new Dictionary<string, double>(),
                    ProcessingTimeMs = response.ProcessingTimeMs,
                    Success = response.Success,
                    ErrorMessage = response.ErrorMessage,
                    Timestamp = DateTime.UtcNow,
                    ExplainabilityData = new Dictionary<string, object>
                    {
                        ["exact_disease"] = response.Prediction
                    }
                };

                _logger.LogInformation(
                    "{Module} detected: {Disease} -> {Category} (confidence: {Confidence:P2})",
                    prediction.ModuleName, response.Prediction,
                    prediction.Prediction, response.Confidence);

                await SaveModulePredictionToDatabase(requestId, prediction, result.Topic, response.ExplainabilityData);

                if (_pendingRequests.TryGetValue(requestId, out var pending))
                {
                    pending.AddPrediction(prediction);
                    _logger.LogInformation("Added {Module} prediction ({Count}/3)",
                        prediction.ModuleName, pending.GetCount());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {Topic}", result.Topic);
            }
        }

        private async Task SaveModulePredictionToDatabase(
            string requestId,
            ModulePrediction prediction,
            string topic,
            JsonElement? explainabilityData)
        {
            try
            {
                if (!Guid.TryParse(requestId.Replace("case-", ""), out var caseId))
                {
                    _logger.LogWarning("Invalid requestId format: {RequestId}", requestId);
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var dbService = scope.ServiceProvider.GetRequiredService<DatabaseIntegrationService>();

                var explainabilityDict = new Dictionary<string, object>
                {
                    ["exact_disease"] = prediction.ExplainabilityData["exact_disease"]?.ToString() ?? "Unknown"
                };

                if (explainabilityData.HasValue && explainabilityData.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in explainabilityData.Value.EnumerateObject())
                    {
                        explainabilityDict[property.Name] = property.Value.ToString();
                    }
                }

                if (topic.Contains("imaging"))
                {
                    await dbService.SaveImagingResultAsync(
                        caseId: caseId,
                        prediction: prediction.ExplainabilityData["exact_disease"]?.ToString() ?? "Unknown",
                        confidence: prediction.Confidence,
                        probabilities: prediction.Probabilities,
                        processingTimeMs: prediction.ProcessingTimeMs,
                        explainabilityData: explainabilityDict,
                        success: prediction.Success,
                        errorMessage: prediction.ErrorMessage
                    );
                }
                else if (topic.Contains("clinical"))
                {
                    await dbService.SaveClinicalResultAsync(
                        caseId: caseId,
                        prediction: prediction.ExplainabilityData["exact_disease"]?.ToString() ?? "Unknown",
                        confidence: prediction.Confidence,
                        probabilities: prediction.Probabilities,
                        processingTimeMs: prediction.ProcessingTimeMs,
                        explainabilityData: explainabilityDict,
                        success: prediction.Success,
                        errorMessage: prediction.ErrorMessage
                    );
                }
                else if (topic.Contains("lab"))
                {
                    await dbService.SaveLaboratoryResultAsync(
                        caseId: caseId,
                        prediction: prediction.ExplainabilityData["exact_disease"]?.ToString() ?? "Unknown",
                        confidence: prediction.Confidence,
                        probabilities: prediction.Probabilities,
                        processingTimeMs: prediction.ProcessingTimeMs,
                        explainabilityData: explainabilityDict,
                        success: prediction.Success,
                        errorMessage: prediction.ErrorMessage
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save module prediction to database");
            }
        }

        public void RegisterRequest(string requestId, int timeoutSeconds = 30, int expectedCount = 3)
        {
            _pendingRequests.TryAdd(requestId, new PendingRequest(timeoutSeconds, expectedCount));
            _logger.LogInformation("Registered request {RequestId} (expecting {Count} modules)", requestId, expectedCount);
        }

        public async Task<List<ModulePrediction>> WaitForResponsesAsync(string requestId, CancellationToken ct)
        {
            if (!_pendingRequests.TryGetValue(requestId, out var pending))
                return new List<ModulePrediction>();

            try
            {
                await pending.WaitForCompletionAsync(ct);
                return pending.GetPredictions();
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Timeout waiting for request {RequestId}. Received {Count}/3 modules",
                    requestId, pending.GetCount());
                return pending.GetPredictions();
            }
            finally
            {
                _pendingRequests.TryRemove(requestId, out _);
            }
        }

        public async Task<EnsembleResult> ProcessEnsembleAsync(
            string requestId,
            List<ModulePrediction> predictions)
        {
            _logger.LogInformation("Processing ensemble fusion with {Count} predictions", predictions.Count);

            ModelPrediction? imagingPrediction = null;
            ModelPrediction? labsPrediction = null;
            ModelPrediction? symptomsPrediction = null;

            foreach (var pred in predictions)
            {
                var exactDisease = pred.ExplainabilityData.TryGetValue("exact_disease", out var disease)
                    ? disease?.ToString() ?? "Unknown"
                    : "Unknown";

                var modelPrediction = new ModelPrediction
                {
                    PredictedDisease = exactDisease,
                    Confidence = pred.Confidence,
                    ProbabilityDistribution = pred.Probabilities,
                    Timestamp = pred.Timestamp
                };

                if (pred.ModuleName.Contains("Imaging"))
                    imagingPrediction = modelPrediction;
                else if (pred.ModuleName.Contains("Laboratory") || pred.ModuleName.Contains("Lab"))
                    labsPrediction = modelPrediction;
                else if (pred.ModuleName.Contains("Clinical"))
                    symptomsPrediction = modelPrediction;
            }

            var ensembleResult = _ensembleFusion.FusePredictions(
                imagingPrediction,
                labsPrediction,
                symptomsPrediction
            );

            _logger.LogInformation(
                "Ensemble result: {Diagnosis} (Confidence: {Confidence:P1})",
                ensembleResult.FinalDiagnosis, ensembleResult.FinalConfidence);

            await SaveEnsembleResultToDatabase(requestId, ensembleResult, predictions);

            return ensembleResult;
        }

        private async Task SaveEnsembleResultToDatabase(
            string requestId,
            EnsembleResult ensembleResult,
            List<ModulePrediction> predictions)
        {
            try
            {
                if (!Guid.TryParse(requestId.Replace("case-", ""), out var caseId))
                {
                    _logger.LogWarning("Invalid requestId format: {RequestId}", requestId);
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var dbService = scope.ServiceProvider.GetRequiredService<DatabaseIntegrationService>();

                var totalProcessingTime = predictions.Sum(p => p.ProcessingTimeMs);

                var ensembleProbabilities = ensembleResult.AlternativeDiagnoses
                    .Select(ad => new { ad.Diagnosis, ad.WeightedScore })
                    .Append(new
                    {
                        Diagnosis = ensembleResult.FinalDiagnosis,
                        WeightedScore = ensembleResult.WeightedScore
                    })
                    .Where(d => !string.IsNullOrEmpty(d.Diagnosis))
                    .ToDictionary(d => d.Diagnosis!, d => d.WeightedScore);

                var contributingModules = predictions.Select(p => p.ModuleName).ToList();
                var riskLevel = DetermineRiskLevel(ensembleResult.FinalDiagnosis, ensembleResult.FinalConfidence);
                var recommendations = GenerateRecommendations(ensembleResult);

                var explainabilitySummary = new Dictionary<string, object>
                {
                    ["agreement_score"] = ensembleResult.AgreementScore,
                    ["is_reliable"] = ensembleResult.IsReliable,
                    ["explanation"] = ensembleResult.Explanation
                };

                var caseStatus = ensembleResult.IsReliable ? "completed" : "completed_with_warnings";

                await dbService.SaveUnifiedDiagnosisResultAsync(
                    caseId: caseId,
                    finalDiagnosis: ensembleResult.FinalDiagnosis,
                    overallConfidence: ensembleResult.FinalConfidence,
                    ensembleProbabilities: ensembleProbabilities,
                    contributingModules: contributingModules,
                    riskLevel: riskLevel,
                    recommendations: recommendations,
                    explainabilitySummary: explainabilitySummary,
                    totalProcessingTimeMs: totalProcessingTime,
                    status: caseStatus
                );

                _logger.LogInformation("Saved unified diagnosis result for case {CaseId}", caseId);

                await dbService.UpdateCaseStatusAsync(caseId, caseStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save ensemble result to database");
            }
        }

        private string DetermineRiskLevel(string diagnosis, double confidence)
        {
            if (diagnosis.Contains("Glioma", StringComparison.OrdinalIgnoreCase) ||
                diagnosis.Contains("lung_cancer", StringComparison.OrdinalIgnoreCase))
                return confidence >= 0.85 ? "high" : "medium";

            if (diagnosis.Contains("Normal", StringComparison.OrdinalIgnoreCase) ||
                diagnosis.Contains("no_cancer", StringComparison.OrdinalIgnoreCase))
                return "low";

            return "medium";
        }

        private List<string> GenerateRecommendations(EnsembleResult result)
        {
            var recommendations = new List<string>();

            var diagnosisRecs = result.FinalDiagnosis.ToLower() switch
            {
                "glioma" => new[]
                {
                    "Urgent neurosurgery referral required",
                    "Biopsy required for grading",
                    "Oncology consultation recommended"
                },
                "meningioma" => new[]
                {
                    "MRI with contrast recommended",
                    "Neurosurgery consultation required",
                    "Monitor for symptom progression"
                },
                "pituitary" => new[]
                {
                    "Endocrinology consultation required",
                    "Visual field testing recommended",
                    "Hormone level assessment needed"
                },
                "normal" => new[]
                {
                    "No immediate action required",
                    "Routine follow-up in 12 months"
                },
                "lung_cancer" => new[]
                {
                    "Urgent oncology referral required",
                    "CT-guided biopsy recommended",
                    "Pulmonology consultation required"
                },
                "no_cancer" => new[]
                {
                    "Continue routine monitoring",
                    "Follow-up in 6 months"
                },
                _ => new[] { "Clinical review recommended" }
            };

            recommendations.AddRange(diagnosisRecs);

            if (!result.IsReliable)
                recommendations.Add("Low confidence - consider additional diagnostic tests");

            return recommendations;
        }

        private string GetModuleName(string topic) => topic switch
        {
            var t when t == BrainImagingResults => "Brain-Imaging",
            var t when t == BrainClinicalResults => "Brain-Clinical",
            var t when t == BrainLabResults => "Brain-Laboratory",
            var t when t == LungImagingResults => "Lung-Imaging",
            var t when t == LungClinicalResults => "Lung-Clinical",
            var t when t == LungLabResults => "Lung-Laboratory",
            _ => "Unknown"
        };

        private DiagnosisType ParseDiagnosis(string prediction, string topic)
        {
            var isLungCancer = topic.Contains("lung");

            if (isLungCancer)
            {
                return prediction?.ToLower() switch
                {
                    "lung_cancer" => DiagnosisType.Malignant,
                    "no_cancer" => DiagnosisType.Benign,
                    _ => DiagnosisType.Inconclusive
                };
            }

            return prediction?.ToLower() switch
            {
                "glioma" => DiagnosisType.Malignant,
                "meningioma" or "pituitary" or "alzheimer_mild" or "alzheimer_moderate" or
                "alzheimer_very_mild" or "multiple_sclerosis" or "normal" => DiagnosisType.Benign,
                _ => DiagnosisType.Inconclusive
            };
        }

        private class PendingRequest
        {
            private readonly List<ModulePrediction> _predictions = new();
            private readonly SemaphoreSlim _signal = new(0, 1);
            private readonly DateTime _expires;
            private readonly object _lock = new();
            private readonly int _expectedCount;

            public PendingRequest(int timeoutSeconds, int expectedCount = 3)
            {
                _expires = DateTime.UtcNow.AddSeconds(timeoutSeconds);
                _expectedCount = expectedCount;
            }

            public void AddPrediction(ModulePrediction prediction)
            {
                lock (_lock)
                {
                    _predictions.Add(prediction);
                    if (_predictions.Count >= _expectedCount)
                        _signal.Release();
                }
            }

            public async Task WaitForCompletionAsync(CancellationToken ct)
            {
                var timeout = _expires - DateTime.UtcNow;
                if (timeout.TotalMilliseconds > 0)
                {
                    var completed = await _signal.WaitAsync(timeout, ct);
                    if (!completed)
                        throw new TimeoutException();
                }
                else
                {
                    throw new TimeoutException();
                }
            }

            public List<ModulePrediction> GetPredictions()
            {
                lock (_lock) { return new List<ModulePrediction>(_predictions); }
            }

            public int GetCount()
            {
                lock (_lock) { return _predictions.Count; }
            }
        }

        private class ModuleResponse
        {
            public string RequestId { get; init; } = string.Empty;
            public string Prediction { get; init; } = string.Empty;
            public double Confidence { get; init; }
            public Dictionary<string, double>? Probabilities { get; init; }
            public double ProcessingTimeMs { get; init; }
            public bool Success { get; init; }
            public string? ErrorMessage { get; init; }
            public JsonElement? ExplainabilityData { get; init; }
        }

        public override void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}