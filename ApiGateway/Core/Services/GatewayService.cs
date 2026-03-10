
using ApiGateway.Ports;

namespace ApiGateway.Core.Services
{
    public interface IGatewayService
    {
        Task<ApiResponse> ProcessRequestAsync(ApiRequest request);

        Task<RateLimitInfo> GetRateLimitInfoAsync(string clientId, string endpoint);
    }
    public class GatewayService : IGatewayService
    {
        private readonly IRateLimiter _rateLimiter;
        private readonly IDownstreamService _downstreamService;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ILogger<GatewayService> _logger;

        public GatewayService(
            IRateLimiter rateLimiter,
            IDownstreamService downstreamService,
            IMessagePublisher messagePublisher,
            ILogger<GatewayService> logger)
        {
            _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
            _downstreamService = downstreamService ?? throw new ArgumentNullException(nameof(downstreamService));
            _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<ApiResponse> ProcessRequestAsync(ApiRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var endpointKey = string.IsNullOrWhiteSpace(request.Endpoint)
                    ? request.Path
                    : request.Endpoint;
                _logger.LogDebug("Checking rate limit for client {ClientId} on {Endpoint}",
                    request.ClientId, endpointKey);
                var allowed = await _rateLimiter.IsRequestAllowedAsync(request.ClientId, endpointKey);
                if (!allowed)
                {
                    _logger.LogWarning("Rate limit exceeded for client {ClientId} on {Endpoint}",
                        request.ClientId, endpointKey);

    
                    _ = Task.Run(() => PublishEventAsync(request, 429, "Rate limit exceeded", stopwatch.ElapsedMilliseconds));

                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Rate limit exceeded. Please try again later.",
                        StatusCode = 429
                    };
                }

                _logger.LogInformation("Forwarding request {RequestId} from client {ClientId} to {Path}",
                    request.RequestId, request.ClientId, request.Path);

                var response = await _downstreamService.ForwardRequestAsync(request);

#pragma warning disable CS4014
                _ = Task.Run(() => PublishEventAsync(request, response.StatusCode, "Request processed", stopwatch.ElapsedMilliseconds));
#pragma warning restore CS4014

                _logger.LogInformation("Request {RequestId} completed with status {StatusCode} in {ElapsedMs}ms",
                    request.RequestId, response.StatusCode, stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request {RequestId} from client {ClientId}",
                    request.RequestId, request.ClientId);

#pragma warning disable CS4014
                _ = Task.Run(() => PublishEventAsync(request, 500, ex.Message, stopwatch.ElapsedMilliseconds));
#pragma warning restore CS4014

                return new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred processing your request",
                    StatusCode = 500
                };
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        public async Task<RateLimitInfo> GetRateLimitInfoAsync(string clientId, string endpoint)
        {
            var remaining = await _rateLimiter.GetRemainingRequestsAsync(clientId, endpoint);

            return new RateLimitInfo
            {
                RemainingRequests = remaining
            };
        }

        private async Task PublishEventAsync(ApiRequest request, int statusCode, string message, long elapsedMs)
        {
            try
            {
                var gatewayEvent = new GatewayEvent
                {
                    RequestId = request.RequestId,
                    ClientId = request.ClientId,
                    Path = request.Path,
                    Method = request.Method,
                    StatusCode = statusCode,
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    ProcessingTimeMs = elapsedMs
                };

                await _messagePublisher.PublishEventAsync("gateway-events", gatewayEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event for request {RequestId}", request.RequestId);
            }
        }
    }
}

public class ApiRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string ClientId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ServiceClusterName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public string? QueryString { get; set; }
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? TargetInstance { get; set; }
    public long? ResponseTime { get; set; }
}

public class GatewayEvent
{
    public string RequestId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public long ProcessingTimeMs { get; set; }
}

public class RateLimitInfo
{
    public int RemainingRequests { get; set; }
}