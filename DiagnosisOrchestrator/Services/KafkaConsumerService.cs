using Confluent.Kafka;
using DiagnosisOrchestrator.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosisOrchestrator.Services
{
    /// <summary>
    /// Kafka consumer service for receiving results from Python AI modules
    /// </summary>
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly KafkaOptions _options;
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        
        // Store pending requests and their collected responses
        private readonly ConcurrentDictionary<string, ModuleResponseCollection> _pendingRequests;

        public KafkaConsumerService(
            IOptions<KafkaOptions> options,
            ILogger<KafkaConsumerService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pendingRequests = new ConcurrentDictionary<string, ModuleResponseCollection>();

            var config = new ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                GroupId = "diagnosis-orchestrator-consumer-group",
                ClientId = _options.ClientId + "-consumer",
                AutoOffsetReset = AutoOffsetReset.Latest,
                EnableAutoCommit = true,
                EnableAutoOffsetStore = true,
                SessionTimeoutMs = 30000,
                HeartbeatIntervalMs = 3000
            };

            _consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, error) =>
                {
                    _logger.LogError("Kafka consumer error: {Reason}", error.Reason);
                })
                .SetPartitionsAssignedHandler((_, partitions) =>
                {
                    _logger.LogInformation("Partitions assigned: {Partitions}",
                        string.Join(", ", partitions));
                })
                .Build();

            // Subscribe to all three result topics from Python modules
            _consumer.Subscribe(new[]
            {
                _options.ImagingResultTopic,
                _options.ClinicalResultTopic,
                _options.LabResultTopic
            });

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _logger.LogInformation("Kafka consumer initialized");
        }

        /// <summary>
        /// Register a new request to track module responses
        /// </summary>
        public void RegisterRequest(string requestId, int timeoutSeconds = 30)
        {
            var collection = new ModuleResponseCollection
            {
                RequestId = requestId,
                CreatedAt = DateTime.UtcNow,
                TimeoutAt = DateTime.UtcNow.AddSeconds(timeoutSeconds)
            };

            _pendingRequests.TryAdd(requestId, collection);
            _logger.LogDebug("Registered request {RequestId} for module response tracking", requestId);
        }

        /// <summary>
        /// Wait for all three module responses for a specific request
        /// </summary>
        public async Task<List<ModulePrediction>> WaitForAllModuleResponsesAsync(
            string requestId,
            CancellationToken cancellationToken = default)
        {
            if (!_pendingRequests.TryGetValue(requestId, out var collection))
            {
                _logger.LogWarning("Request {RequestId} not found in pending requests", requestId);
                return new List<ModulePrediction>();
            }

            var startTime = DateTime.UtcNow;
            var timeout = collection.TimeoutAt - startTime;

            _logger.LogInformation(
                "Waiting for module responses for request {RequestId} (timeout: {Timeout}s)",
                requestId, timeout.TotalSeconds);

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                // Wait until all 3 responses received or timeout
                while (!collection.IsComplete && !cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(100, cts.Token);
                }

                var processingTime = DateTime.UtcNow - startTime;

                if (collection.IsComplete)
                {
                    _logger.LogInformation(
                        "All module responses received for request {RequestId} in {Time}ms",
                        requestId, processingTime.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning(
                        "Timeout waiting for responses for request {RequestId} - " +
                        "Received: Imaging={Imaging}, Clinical={Clinical}, Lab={Lab}",
                        requestId,
                        collection.ImagingResponse != null,
                        collection.ClinicalResponse != null,
                        collection.LabResponse != null);
                }

                // Convert to ModulePrediction objects
                return collection.ToModulePredictions();
            }
            finally
            {
                _pendingRequests.TryRemove(requestId, out _);
            }
        }

        /// <summary>
        /// Background service that continuously consumes messages from Kafka
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Kafka consumer starting, subscribing to topics: {Topics}",
                string.Join(", ", new[] { 
                    _options.ImagingResultTopic, 
                    _options.ClinicalResultTopic, 
                    _options.LabResultTopic 
                }));

            // CRITICAL: Yield to prevent blocking the application startup
            await Task.Yield();

            try
            {
                // Run the consumer loop in the background
                await Task.Run(async () =>
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(100));

                            if (consumeResult != null)
                            {
                                await ProcessMessageAsync(consumeResult);
                            }

                            // Clean up expired requests periodically
                            CleanupExpiredRequests();
                            
                            // Small delay to prevent tight loop
                            await Task.Delay(10, stoppingToken);
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError(ex, "Error consuming message: {Error}", ex.Error.Reason);
                            await Task.Delay(1000, stoppingToken); // Back off on error
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error in consumer loop");
                            await Task.Delay(1000, stoppingToken); // Back off on error
                        }
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka consumer service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Kafka consumer service - service will stop but app will continue");
                // Don't rethrow - let the app continue even if consumer fails
            }
            finally
            {
                try
                {
                    _consumer.Close();
                    _logger.LogInformation("Kafka consumer service stopped");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing Kafka consumer");
                }
            }
        }

        /// <summary>
        /// Process incoming Python module response
        /// </summary>
        private Task ProcessMessageAsync(ConsumeResult<string, string> result)
        {
            try
            {
                // Parse Python response
                var pythonResponse = JsonSerializer.Deserialize<PythonModuleResponse>(
                    result.Message.Value, _jsonOptions);

                if (pythonResponse == null)
                {
                    _logger.LogWarning("Failed to deserialize message from {Topic}", result.Topic);
                    return Task.CompletedTask;
                }

                _logger.LogDebug(
                    "Received {Module} result for request {RequestId} - Status: {Status}",
                    pythonResponse.Module, pythonResponse.RequestId, pythonResponse.Status);

                // Find the corresponding pending request
                if (_pendingRequests.TryGetValue(pythonResponse.RequestId, out var collection))
                {
                    // Add response to appropriate slot based on module type
                    switch (pythonResponse.Module?.ToLowerInvariant())
                    {
                        case "imaging":
                            collection.ImagingResponse = pythonResponse;
                            break;
                        case "clinical":
                            collection.ClinicalResponse = pythonResponse;
                            break;
                        case "laboratory":
                            collection.LabResponse = pythonResponse;
                            break;
                        default:
                            _logger.LogWarning("Unknown module type: {Module}", pythonResponse.Module);
                            break;
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Received result for unknown/expired request {RequestId}",
                        pythonResponse.RequestId);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON from {Topic}", result.Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {Topic}", result.Topic);
            }

            return Task.CompletedTask;
        }

        private void CleanupExpiredRequests()
        {
            var now = DateTime.UtcNow;
            var expiredRequests = _pendingRequests
                .Where(kvp => now > kvp.Value.TimeoutAt)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var requestId in expiredRequests)
            {
                if (_pendingRequests.TryRemove(requestId, out _))
                {
                    _logger.LogWarning("Cleaned up expired request {RequestId}", requestId);
                }
            }
        }

        public override void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// Python module response format
    /// </summary>
    internal class PythonModuleResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public PythonPrediction? Prediction { get; set; }
        public string? Error { get; set; }
    }

    internal class PythonPrediction
    {
        public int PredictedClass { get; set; }
        public string? DiseaseName { get; set; }
        public double Confidence { get; set; }
        public List<double> Probabilities { get; set; } = new();
        public Dictionary<string, double>? TopFeatures { get; set; }
    }

    /// <summary>
    /// Collection of module responses for a single request
    /// </summary>
    internal class ModuleResponseCollection
    {
        public string RequestId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime TimeoutAt { get; set; }

        public PythonModuleResponse? ImagingResponse { get; set; }
        public PythonModuleResponse? ClinicalResponse { get; set; }
        public PythonModuleResponse? LabResponse { get; set; }

        public bool IsComplete =>
            ImagingResponse != null &&
            ClinicalResponse != null &&
            LabResponse != null;

        /// <summary>
        /// Convert Python responses to ModulePrediction objects
        /// </summary>
        public List<ModulePrediction> ToModulePredictions()
        {
            var predictions = new List<ModulePrediction>();

            if (ImagingResponse != null)
            {
                predictions.Add(ConvertToPrediction("Imaging", ImagingResponse));
            }

            if (ClinicalResponse != null)
            {
                predictions.Add(ConvertToPrediction("Clinical", ClinicalResponse));
            }

            if (LabResponse != null)
            {
                predictions.Add(ConvertToPrediction("Laboratory", LabResponse));
            }

            return predictions;
        }

        private ModulePrediction ConvertToPrediction(string moduleName, PythonModuleResponse response)
        {
            var success = response.Status == "success";
            
            if (!success || response.Prediction == null)
            {
                return new ModulePrediction
                {
                    ModuleName = moduleName,
                    Prediction = DiagnosisType.Inconclusive,
                    Confidence = 0.0,
                    Probabilities = new Dictionary<string, double>(),
                    ProcessingTimeMs = 0,
                    Success = false,
                    ErrorMessage = response.Error ?? "Unknown error"
                };
            }

            // Map Python predicted class to DiagnosisType
            var diagnosisType = MapPredictedClassToDiagnosisType(response.Prediction.PredictedClass);

            // Convert probabilities list to dictionary
            var probabilities = new Dictionary<string, double>();
            for (int i = 0; i < response.Prediction.Probabilities.Count; i++)
            {
                probabilities[$"class_{i}"] = response.Prediction.Probabilities[i];
            }

            var explainability = new Dictionary<string, object>();
            if (response.Prediction.TopFeatures != null)
            {
                explainability["top_features"] = response.Prediction.TopFeatures;
            }

            return new ModulePrediction
            {
                ModuleName = moduleName,
                Prediction = diagnosisType,
                Confidence = response.Prediction.Confidence,
                Probabilities = probabilities,
                ProcessingTimeMs = 0, // Python modules don't return this
                ExplainabilityData = explainability.Count > 0 ? explainability : null,
                Success = true
            };
        }

        private DiagnosisType MapPredictedClassToDiagnosisType(int predictedClass)
        {
            // Map Python predicted classes to your DiagnosisType enum
            // Adjust this mapping based on your model's output classes
            return predictedClass switch
            {
                0 => DiagnosisType.Benign,
                1 => DiagnosisType.Malignant,
                _ => DiagnosisType.Inconclusive
            };
        }
    }
}