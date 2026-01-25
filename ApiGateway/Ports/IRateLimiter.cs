namespace ApiGateway.Ports;

public interface IRateLimiter
{

    Task<bool> IsRequestAllowedAsync(string clientId, string endpoint);
    Task<int> GetRemainingRequestsAsync(string clientId, string endpoint);
    Task ResetLimitAsync(string clientId, string endpoint);
}