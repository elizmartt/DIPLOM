using ApiGateway.Ports;
using Confluent.Kafka;
using System.Text.Json;

namespace ApiGateway.Adapters.Outbound.Messaging;

public class KafkaMessagePublisher : IMessagePublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaMessagePublisher(string bootstrapServers)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100
        };

        _producer = new ProducerBuilder<string, string>(config).Build();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task PublishAsync(string topic, string message, string? key = null)
    {
        try
        {
            var kafkaMessage = new Message<string, string>
            {
                Key = key ?? Guid.NewGuid().ToString(),
                Value = message,
                Timestamp = Timestamp.Default
            };
            var result = await _producer.ProduceAsync(topic, kafkaMessage);
            Console.WriteLine($"Message delivered to {result.TopicPartitionOffset}");
        }
        catch (ProduceException<string, string> ex)
        {
            Console.WriteLine($"Failed to deliver message: {ex.Error.Reason}");
            throw;
        }
    }

    public async Task PublishEventAsync<T>(string topic, T eventData) where T : class
    {
        var json = JsonSerializer.Serialize(eventData, _jsonOptions);
        var key = GetEventKey(eventData);

        await PublishAsync(topic, json, key);
    }

    private string GetEventKey<T>(T eventData)
    {
        var properties = typeof(T).GetProperties();

        var idProperty = properties.FirstOrDefault(p =>
            p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));

        if (idProperty != null)
        {
            var value = idProperty.GetValue(eventData);
            if (value != null)
            {
                return value.ToString() ?? Guid.NewGuid().ToString();
            }
        }

        return Guid.NewGuid().ToString();
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}