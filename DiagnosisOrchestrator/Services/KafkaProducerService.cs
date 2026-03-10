using Confluent.Kafka;
using System.Text.Json;
using DiagnosisOrchestrator.Models;

namespace DiagnosisOrchestrator.Services
{
    public class KafkaProducerService : IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducerService> _logger;
        private readonly IConfiguration _configuration;

        // Brain tumor topics
        private const string BRAIN_IMAGING_TOPIC = "brain-imaging-requests";
        private const string BRAIN_CLINICAL_TOPIC = "brain-clinical-requests";
        private const string BRAIN_LAB_TOPIC = "brain-lab-requests";

        // Lung cancer topics
        private const string LUNG_IMAGING_TOPIC = "lung-imaging-requests";
        private const string LUNG_CLINICAL_TOPIC = "lung-clinical-requests";
        private const string LUNG_LAB_TOPIC = "lung-lab-requests";

        public KafkaProducerService(ILogger<KafkaProducerService> logger, IConfiguration configuration)
        {
            _logger = logger;

            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                ClientId = "diagnosis-orchestrator-producer",
                Acks = Acks.Leader,  
                EnableIdempotence = false,  
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 100
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _logger.LogInformation("Kafka producer initialized with server: {Server}", 
                configuration["Kafka:BootstrapServers"] ?? "localhost:9092");
        }

        public async Task PublishBrainDiagnosisRequestAsync(DiagnosisRequest request)
        {
            await PublishDiagnosisRequestAsync(
                request,
                BRAIN_IMAGING_TOPIC,
                BRAIN_CLINICAL_TOPIC,
                BRAIN_LAB_TOPIC,
                "Brain");
        }

        public async Task PublishLungDiagnosisRequestAsync(DiagnosisRequest request)
        {
            await PublishDiagnosisRequestAsync(
                request,
                LUNG_IMAGING_TOPIC,
                LUNG_CLINICAL_TOPIC,
                LUNG_LAB_TOPIC,
                "Lung");
        }
    
        public async Task PublishToTopicAsync(string topic, object payload)
        {
            try
            {
                var key = Guid.NewGuid().ToString(); 
                await PublishAsync(topic, key, payload);
                _logger.LogInformation("Successfully published message to topic: {Topic}", topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing to topic: {Topic}", topic);
                throw;
            }
        }
        private async Task PublishDiagnosisRequestAsync(
            DiagnosisRequest request,
            string imagingTopic,
            string clinicalTopic,
            string labTopic,
            string diagnosisType)
        {
            var requestId = request.DiagnosisCaseId.ToString();
            _logger.LogInformation("Publishing {Type} diagnosis request {RequestId} to Kafka", 
                diagnosisType, requestId);

            var tasks = new List<Task>();

            if (request.ImagingData != null)
            {
                var payload = new
                {
                    requestId = requestId,
                    diagnosisCaseId = request.DiagnosisCaseId,
                    patientId = request.PatientId,
                    imagingData = request.ImagingData
                };

                tasks.Add(PublishAsync(imagingTopic, requestId, payload));
            }

            if (request.ClinicalData != null)
            {
                var payload = new
                {
                    requestId = requestId,
                    diagnosisCaseId = request.DiagnosisCaseId,
                    patientId = request.PatientId,
                    clinicalData = request.ClinicalData
                };

                tasks.Add(PublishAsync(clinicalTopic, requestId, payload));
            }

            if (request.LaboratoryData != null)
            {
                var payload = new
                {
                    requestId = requestId,
                    diagnosisCaseId = request.DiagnosisCaseId,
                    patientId = request.PatientId,
                    laboratoryData = request.LaboratoryData
                };

                tasks.Add(PublishAsync(labTopic, requestId, payload));
            }

            await Task.WhenAll(tasks);
            _logger.LogInformation("Published {Type} request {RequestId} to {Count} topics",
                diagnosisType, requestId, tasks.Count);
        }

        private async Task PublishAsync(string topic, string key, object payload)
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            };

            var result = await _producer.ProduceAsync(topic, message);
            _logger.LogInformation("Published to {Topic} - Partition: {Partition}, Offset: {Offset}",
                topic, result.Partition.Value, result.Offset.Value);
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
    }
}