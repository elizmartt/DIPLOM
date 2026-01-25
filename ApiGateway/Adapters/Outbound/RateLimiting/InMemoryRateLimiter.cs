using ApiGateway.Ports;
using System.Collections.Concurrent;

namespace ApiGateway.Adapters.Outbound.RateLimiting;

public class InMemoryRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, ClientRateLimit> _rateLimits = new();
    private readonly int _requestsPerMinute;
    private readonly TimeSpan _windowSize;

    public InMemoryRateLimiter(int requestsPerMinute = 60)
    {
        _requestsPerMinute = requestsPerMinute;
        _windowSize = TimeSpan.FromMinutes(1);
    }

    public Task<bool> IsRequestAllowedAsync(string clientId, string endpoint)
    {
        var key = GetKey(clientId, endpoint);
        var now = DateTime.UtcNow;

        var rateLimit = _rateLimits.GetOrAdd(key, _ => new ClientRateLimit
        {
            WindowStart = now,
            RequestCount = 0
        });

        if (now - rateLimit.WindowStart >= _windowSize)
        {
            rateLimit.WindowStart = now;
            rateLimit.RequestCount = 0;
        }

        if (rateLimit.RequestCount >= _requestsPerMinute)
        {
            return Task.FromResult(false);
        }

        rateLimit.RequestCount++;
        return Task.FromResult(true);
    }

    public Task<int> GetRemainingRequestsAsync(string clientId, string endpoint)
    {
        var key = GetKey(clientId, endpoint);
        var now = DateTime.UtcNow;

        if (!_rateLimits.TryGetValue(key, out var rateLimit))
        {
            return Task.FromResult(_requestsPerMinute);
        }

        if (now - rateLimit.WindowStart >= _windowSize)
        {
            return Task.FromResult(_requestsPerMinute);
        }

        var remaining = Math.Max(0, _requestsPerMinute - rateLimit.RequestCount);
        return Task.FromResult(remaining);
    }

    public Task ResetLimitAsync(string clientId, string endpoint)
    {
        var key = GetKey(clientId, endpoint);
        _rateLimits.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private string GetKey(string clientId, string endpoint)
    {
        return $"{clientId}:{endpoint}";
    }

    private class ClientRateLimit
    {
        public DateTime WindowStart { get; set; }
        public int RequestCount { get; set; }
    }
}