using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApiGateway.Core.Services;
using ApiGateway.Ports;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text; 
using ApiGateway; 

[ApiController]
public class GatewayController : ControllerBase
{
    private readonly IGatewayService _gatewayService;
    private readonly IRateLimiter _rateLimiter;
    private readonly List<string> _serviceNames;
    public GatewayController(
        IGatewayService gatewayService,
        IRateLimiter rateLimiter,
        IOptions<ServiceClusterConfiguration> config
    )
    {
        _gatewayService = gatewayService;
        _rateLimiter = rateLimiter;
        _serviceNames = config.Value.GetServiceNames();
    }

    [HttpGet("gateway/test")]
    public IActionResult Test()
    {
        return Ok(new { message = "Gateway test OK", time = DateTime.UtcNow });
    }

    [HttpGet("gateway/health")]
    public async Task<IActionResult> Health()
    {
        var clientId = Request.Headers["X-Client-Id"].FirstOrDefault() ?? "anonymous";
        var endpoint = "/health";

        if (!await _rateLimiter.IsRequestAllowedAsync(clientId, endpoint))
        {
            return StatusCode(429, new { message = "Rate limit exceeded" });
        }
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("gateway/ratelimit")]
    public async Task<IActionResult> GetRateLimitInfo([FromQuery] string endpoint = "/api")
    {
        var clientId = Request.Headers["X-Client-Id"].FirstOrDefault() ?? "anonymous";
        var info = await _gatewayService.GetRateLimitInfoAsync(clientId, endpoint);
        return Ok(info);
    }
    [Route("[action]")]
    public async Task<IActionResult> ForwardRequest()
    {
        var fullPath = HttpContext.Request.Path.Value ?? string.Empty;

        // Service Routing
        const string expectedPrefix = "/gateway/api/";

        if (!fullPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = $"Route not supported by gateway: {fullPath}" });
        }

        var pathPayload = fullPath.Substring(expectedPrefix.Length);
        var requestPathSegments = pathPayload.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (requestPathSegments.Length == 0)
        {
            return BadRequest(new { message = "Missing service identifier in API path." });
        }

        string pathPrefix = requestPathSegments.First().ToLower();
        string? targetServiceClusterName = null;

        foreach (var serviceName in _serviceNames)
        {
            var servicePrefix = serviceName.Replace("Service", "", StringComparison.OrdinalIgnoreCase).ToLower();

            if (pathPrefix.Equals(servicePrefix, StringComparison.OrdinalIgnoreCase))
            {
                targetServiceClusterName = serviceName;
                break;
            }
        }

        if (targetServiceClusterName == null)
        {
            return NotFound(new { message = $"No configured service cluster found for path prefix: /{pathPrefix}" });
        }

        // rate limiting
        var clientId = Request.Headers["X-Client-Id"].FirstOrDefault() ?? "anonymous";
        var endpoint = "/api/" + pathPayload;

        if (!await _rateLimiter.IsRequestAllowedAsync(clientId, endpoint))
        {
            return StatusCode(429, new { message = "Rate limit exceeded" });
        }

        string? body = null;
        if (HttpContext.Request.ContentLength > 0)
        {
            HttpContext.Request.EnableBuffering();
            using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
            body = await reader.ReadToEndAsync();
            HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
        }

        var apiRequest = new ApiRequest
        {
            ClientId = clientId,
            ServiceClusterName = targetServiceClusterName,
            Endpoint = endpoint,
            Path = endpoint,
            Method = HttpContext.Request.Method,
            Body = body,
            Headers = HttpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
        };
        var response = await _gatewayService.ProcessRequestAsync(apiRequest);
        var rateLimitInfo = await _gatewayService.GetRateLimitInfoAsync(clientId, endpoint);
        Response.Headers["X-RateLimit-Remaining"] = rateLimitInfo.RemainingRequests.ToString();

        if (!string.IsNullOrEmpty(response.TargetInstance))
            Response.Headers["X-Target-Instance"] = response.TargetInstance;
        if (response.ResponseTime.HasValue)
            Response.Headers["X-Downstream-Response-Time-Ms"] = response.ResponseTime.Value.ToString();
        Response.StatusCode = response.StatusCode;

        return Content(
            response.Message,
            "application/json" 
        );
    }
}
