namespace ApiGateway.Ports;
public interface IMessagePublisher
{

    Task PublishAsync(string topic, string message, string? key = null);
    Task PublishEventAsync<T>(string topic, T eventData) where T : class;
}