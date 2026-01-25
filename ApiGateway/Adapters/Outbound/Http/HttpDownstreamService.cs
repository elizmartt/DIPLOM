using System.Text;
using ApiGateway.Ports;

namespace ApiGateway.Adapters.Outbound.Http
{
    public class HttpDownstreamService : IDownstreamService
    {
        private readonly HttpClient _httpClient;
        private readonly ILoadBalancer _loadBalancer;

        public HttpDownstreamService(HttpClient httpClient, ILoadBalancer loadBalancer)
        {
            _httpClient = httpClient;
            _loadBalancer = loadBalancer;
        }

        public async Task<ApiResponse> ForwardRequestAsync(ApiRequest request)
        {
            string serviceName = ResolveServiceNameFromPath(request.Path);
            string baseUrl = _loadBalancer.ResolveEndpoint(serviceName);

            string url = baseUrl.TrimEnd('/') + request.Path;

            if (!string.IsNullOrEmpty(request.QueryString))
                url += "?" + request.QueryString.TrimStart('?');

            var method = new HttpMethod(request.Method);
            var msg = new HttpRequestMessage(method, url);

            if (!string.IsNullOrEmpty(request.Body) &&
                (method == HttpMethod.Post || method == HttpMethod.Put || method.Method == "PATCH"))
            {
                msg.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
            }

            foreach (var h in request.Headers)
                msg.Headers.TryAddWithoutValidation(h.Key, h.Value);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var resp = await _httpClient.SendAsync(msg);
            var content = await resp.Content.ReadAsStringAsync();
            sw.Stop();

            return new ApiResponse
            {
                Success = resp.IsSuccessStatusCode,
                StatusCode = (int)resp.StatusCode,
                Message = content,
                TargetInstance = baseUrl,
                ResponseTime = sw.ElapsedMilliseconds
            };
        }

        private string ResolveServiceNameFromPath(string path)
        {
            var p = path.ToLower();

            if (p.StartsWith("/api/diagnosis")) return "DiagnosisService";
            if (p.StartsWith("/api/doctors")) return "DoctorService";
            if (p.StartsWith("/api/ai/image")) return "ImageAnalysisService";
            if (p.StartsWith("/api/ai/symptom")) return "SymptomAnalysisService";
            if (p.StartsWith("/api/ai/lab")) return "LabAnalysisService";
            if (p.StartsWith("/api/notifications")) return "NotificationService";
            if (p.StartsWith("/api/audit")) return "AuditService";

            throw new InvalidOperationException($"Cannot resolve service for path '{path}'.");
        }
    }
}
