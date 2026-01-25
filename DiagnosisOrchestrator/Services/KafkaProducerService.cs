using Confluent.Kafka;
using DiagnosisOrchestrator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiagnosisOrchestrator.Services
{
    /// <summary>
    /// Kafka producer service interface
    /// </summary>
    public interface IKafkaProducerService
    {
        Task PublishDiagnosisRequestAsync(DiagnosisRequest request);
        Task PublishDiagnosisResultAsync(UnifiedDiagnosis diagnosis);
    }

    /// <summary>
    /// Kafka configuration options
    /// </summary>
    public class KafkaOptions
    {
        public required string BootstrapServers { get; set; }
        
        // Request topics (to Python AI modules)
        public string ImagingRequestTopic { get; set; } = "imaging-requests";
        public string ClinicalRequestTopic { get; set; } = "clinical-requests";
        public string LabRequestTopic { get; set; } = "lab-requests";
        
        // Result topics (from Python AI modules)
        public string ImagingResultTopic { get; set; } = "imaging-results";
        public string ClinicalResultTopic { get; set; } = "clinical-results";
        public string LabResultTopic { get; set; } = "lab-results";
        
        // Legacy topics (keep for backwards compatibility)
        public string DiagnosisRequestTopic { get; set; } = "diagnosis-requests";
        public string DiagnosisResultTopic { get; set; } = "diagnosis-results";
        
        public string ClientId { get; set; } = "diagnosis-orchestrator";
        public int MessageTimeoutMs { get; set; } = 10000;
    }

    /// <summary>
    /// Kafka producer for publishing diagnosis messages to AI modules
    /// </summary>
    public class KafkaProducerService : IKafkaProducerService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly KafkaOptions _options;
        private readonly ILogger<KafkaProducerService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public KafkaProducerService(
            IOptions<KafkaOptions> options,
            ILogger<KafkaProducerService> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var config = new ProducerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                ClientId = _options.ClientId,
                Acks = Acks.All,  // FIXED: Changed from Acks.Leader for idempotence
                MessageTimeoutMs = _options.MessageTimeoutMs,
                EnableIdempotence = true,
                CompressionType = CompressionType.Snappy
            };

            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, error) =>
                {
                    _logger.LogError("Kafka error: {Reason}", error.Reason);
                })
                .Build();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            _logger.LogInformation("Kafka producer initialized: {Servers}", _options.BootstrapServers);
        }

        /// <summary>
        /// Publish diagnosis request to all three AI module topics
        /// </summary>
        public async Task PublishDiagnosisRequestAsync(DiagnosisRequest request)
        {
            var requestId = request.DiagnosisCaseId.ToString();
            var patientId = request.PatientId.ToString();

            try
            {
                // Send to all three modules in parallel
                var tasks = new[]
                {
                    SendImagingRequestAsync(requestId, patientId, request.ImagingData),
                    SendClinicalRequestAsync(requestId, patientId, request.ClinicalData),
                    SendLabRequestAsync(requestId, patientId, request.LaboratoryData)
                };

                await Task.WhenAll(tasks);

                _logger.LogInformation(
                    "Published diagnosis request {RequestId} to all three AI modules",
                    requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish diagnosis request {RequestId}",
                    requestId);
                throw;
            }
        }

        /// <summary>
        /// Send imaging request to Python imaging module
        /// </summary>
        private async Task SendImagingRequestAsync(string requestId, string patientId, ImagingData? data)
        {
            if (data == null)
            {
                _logger.LogWarning("No imaging data provided for request {RequestId}", requestId);
                return;
            }

            var pythonRequest = new
            {
                request_id = requestId,
                patient_id = patientId,
                timestamp = DateTime.UtcNow.ToString("O"),
                image_data = data.ImagePath  // In production, this should be base64 encoded image
            };

            await PublishToTopicAsync(
                _options.ImagingRequestTopic,
                requestId,
                pythonRequest,
                "imaging-request");
        }

        /// <summary>
        /// Send clinical request to Python clinical module
        /// </summary>
        private async Task SendClinicalRequestAsync(string requestId, string patientId, ClinicalData? data)
        {
            if (data == null)
            {
                _logger.LogWarning("No clinical data provided for request {RequestId}", requestId);
                return;
            }

            var pythonRequest = new
            {
                request_id = requestId,
                patient_id = patientId,
                timestamp = DateTime.UtcNow.ToString("O"),
                symptoms = data.Symptoms
            };

            await PublishToTopicAsync(
                _options.ClinicalRequestTopic,
                requestId,
                pythonRequest,
                "clinical-request");
        }

        /// <summary>
        /// Send lab request to Python laboratory module
        /// </summary>
        private async Task SendLabRequestAsync(string requestId, string patientId, LaboratoryData? data)
        {
            if (data == null)
            {
                _logger.LogWarning("No laboratory data provided for request {RequestId}", requestId);
                return;
            }

            var pythonRequest = new
            {
                request_id = requestId,
                patient_id = patientId,
                timestamp = DateTime.UtcNow.ToString("O"),
                lab_results = data.TumorMarkers
            };

            await PublishToTopicAsync(
                _options.LabRequestTopic,
                requestId,
                pythonRequest,
                "lab-request");
        }

        /// <summary>
        /// Generic method to publish to a Kafka topic
        /// </summary>
        private async Task PublishToTopicAsync(string topic, string key, object payload, string messageType)
        {
            try
            {
                var message = new Message<string, string>
                {
                    Key = key,
                    Value = JsonSerializer.Serialize(payload, _jsonOptions),
                    Headers = new Headers
                    {
                        { "message-type", System.Text.Encoding.UTF8.GetBytes(messageType) },
                        { "correlation-id", System.Text.Encoding.UTF8.GetBytes(key) },
                        { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                    }
                };

                var result = await _producer.ProduceAsync(topic, message);

                _logger.LogDebug(
                    "Published {MessageType} to {Topic} - Partition: {Partition}, Offset: {Offset}",
                    messageType,
                    topic,
                    result.Partition.Value,
                    result.Offset.Value);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish to {Topic}: {Reason}",
                    topic,
                    ex.Error.Reason);
                throw;
            }
        }

        /// <summary>
        /// Publish final unified diagnosis result
        /// </summary>
        public async Task PublishDiagnosisResultAsync(UnifiedDiagnosis diagnosis)
        {
            try
            {
                var message = new Message<string, string>
                {
                    Key = diagnosis.DiagnosisCaseId.ToString(),
                    Value = JsonSerializer.Serialize(diagnosis, _jsonOptions),
                    Headers = new Headers
                    {
                        { "message-type", System.Text.Encoding.UTF8.GetBytes("diagnosis-result") },
                        { "correlation-id", System.Text.Encoding.UTF8.GetBytes(diagnosis.DiagnosisCaseId.ToString()) },
                        { "diagnosis", System.Text.Encoding.UTF8.GetBytes(diagnosis.FinalDiagnosis.ToString()) },
                        { "confidence", System.Text.Encoding.UTF8.GetBytes(diagnosis.OverallConfidence.ToString("F4")) },
                        { "timestamp", System.Text.Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) }
                    }
                };

                var result = await _producer.ProduceAsync(_options.DiagnosisResultTopic, message);

                _logger.LogInformation(
                    "Published diagnosis result for case {CaseId}: {Diagnosis} ({Confidence:P2}) to partition {Partition}",
                    diagnosis.DiagnosisCaseId,
                    diagnosis.FinalDiagnosis,
                    diagnosis.OverallConfidence,
                    result.Partition.Value);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish diagnosis result for case {CaseId}: {Reason}",
                    diagnosis.DiagnosisCaseId,
                    ex.Error.Reason);
                throw;
            }
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing Kafka producer");
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}