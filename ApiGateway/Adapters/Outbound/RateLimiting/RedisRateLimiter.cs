using ApiGateway.Ports;
using StackExchange.Redis;

namespace ApiGateway.Adapters.Outbound.RateLimiting;

public class RedisRateLimiter : IRateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly int _requestsPerMinute;
    private readonly TimeSpan _windowSize;

    public RedisRateLimiter(IConnectionMultiplexer redis, int requestsPerMinute = 60)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _db = _redis.GetDatabase();
        _requestsPerMinute = requestsPerMinute;
        _windowSize = TimeSpan.FromMinutes(1);
    }

    public async Task<bool> IsRequestAllowedAsync(string clientId, string endpoint)
    {
        var key = GetRedisKey(clientId, endpoint);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        try
        {
            var transaction = _db.CreateTransaction();
            
            var windowStart = now - (long)_windowSize.TotalSeconds;
            var removeTask = transaction.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
            
            var countTask = transaction.SortedSetLengthAsync(key);
            
            var addTask = transaction.SortedSetAddAsync(key, now.ToString(), now);
            
            var expireTask = transaction.KeyExpireAsync(key, _windowSize);
            
            await transaction.ExecuteAsync();
            
            var currentCount = await countTask;
            
            if (currentCount > _requestsPerMinute)
            {
                await _db.SortedSetRemoveAsync(key, now.ToString());
                return false;
            }
            return true;
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Redis error in rate limiter: {ex.Message}");
            return true;
        }
    }

    public async Task<int> GetRemainingRequestsAsync(string clientId, string endpoint)
    {
        var key = GetRedisKey(clientId, endpoint);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = now - (long)_windowSize.TotalSeconds;
        
        try
        {
            await _db.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
            var currentCount = await _db.SortedSetLengthAsync(key);
            var remaining = Math.Max(0, _requestsPerMinute - (int)currentCount);
            return remaining;
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Redis error getting remaining requests: {ex.Message}");
            return _requestsPerMinute; 
        }
    }

    public async Task ResetLimitAsync(string clientId, string endpoint)
    {
        var key = GetRedisKey(clientId, endpoint);
        
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Redis error resetting limit: {ex.Message}");
        }
    }

    private string GetRedisKey(string clientId, string endpoint)
    {
        return $"ratelimit:{clientId}:{endpoint}";
    }

 
    public async Task<RateLimitInfo> GetRateLimitInfoAsync(string clientId, string endpoint)
    {
        var key = GetRedisKey(clientId, endpoint);
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = now - (long)_windowSize.TotalSeconds;
        
        try
        {
            await _db.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
            var currentCount = await _db.SortedSetLengthAsync(key);
            var ttl = await _db.KeyTimeToLiveAsync(key);
            
            return new RateLimitInfo
            {
                ClientId = clientId,
                Endpoint = endpoint,
                CurrentRequests = (int)currentCount,
                RemainingRequests = Math.Max(0, _requestsPerMinute - (int)currentCount),
                Limit = _requestsPerMinute,
                WindowResetIn = ttl ?? TimeSpan.Zero
            };
        }
        catch (RedisException ex)
        {
            Console.WriteLine($"Redis error getting rate limit info: {ex.Message}");
            return new RateLimitInfo
            {
                ClientId = clientId,
                Endpoint = endpoint,
                CurrentRequests = 0,
                RemainingRequests = _requestsPerMinute,
                Limit = _requestsPerMinute,
                WindowResetIn = TimeSpan.Zero
            };
        }
    }
}

public class RateLimitInfo
{
    public string ClientId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int CurrentRequests { get; set; }
    public int RemainingRequests { get; set; }
    public int Limit { get; set; }
    public TimeSpan WindowResetIn { get; set; }
}