using DiagnosisOrchestrator.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosisOrchestrator.Services
{
    /// <summary>
    /// Service interface for calling AI modules
    /// </summary>
    public interface IModuleClientService
    {
        Task<ModulePrediction?> CallImagingModuleAsync(ImagingData data, CancellationToken cancellationToken = default);
        Task<ModulePrediction?> CallClinicalModuleAsync(ClinicalData data, CancellationToken cancellationToken = default);
        Task<ModulePrediction?> CallLaboratoryModuleAsync(LaboratoryData data, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// HTTP client service for calling Python AI modules
    /// </summary>
    public class ModuleClientService : IModuleClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ModuleEndpoints _endpoints;
        private readonly ILogger<ModuleClientService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ModuleClientService(
            HttpClient httpClient,
            IOptions<ModuleEndpoints> endpoints,
            ILogger<ModuleClientService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _endpoints = endpoints?.Value ?? throw new ArgumentNullException(nameof(endpoints));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<ModulePrediction?> CallImagingModuleAsync(
            ImagingData data, 
            CancellationToken cancellationToken = default)
        {
            return await CallModuleAsync(
                "imaging",
                $"{_endpoints.ImagingServiceUrl}/predict",
                data,
                cancellationToken);
        }

        public async Task<ModulePrediction?> CallClinicalModuleAsync(
            ClinicalData data,
            CancellationToken cancellationToken = default)
        {
            return await CallModuleAsync(
                "clinical",
                $"{_endpoints.ClinicalServiceUrl}/predict",
                data,
                cancellationToken);
        }

        public async Task<ModulePrediction?> CallLaboratoryModuleAsync(
            LaboratoryData data,
            CancellationToken cancellationToken = default)
        {
            return await CallModuleAsync(
                "laboratory",
                $"{_endpoints.LaboratoryServiceUrl}/predict",
                data,
                cancellationToken);
        }

        private async Task<ModulePrediction?> CallModuleAsync<T>(
            string moduleName,
            string endpoint,
            T data,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Calling {Module} module at {Endpoint}", moduleName, endpoint);

                var response = await _httpClient.PostAsJsonAsync(
                    endpoint,
                    data,
                    _jsonOptions,
                    cancellationToken);

                stopwatch.Stop();

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "{Module} module returned {StatusCode}: {Error}",
                        moduleName,
                        response.StatusCode,
                        errorContent);

                    return new ModulePrediction
                    {
                        ModuleName = moduleName,
                        Prediction = DiagnosisType.Inconclusive,
                        Confidence = 0.0,
                        Probabilities = new Dictionary<string, double>(),
                        ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                        Success = false,
                        ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}"
                    };
                }

                var result = await response.Content.ReadFromJsonAsync<ModuleResponse>(_jsonOptions, cancellationToken);

                if (result == null)
                {
                    _logger.LogError("{Module} module returned null response", moduleName);
                    return CreateErrorPrediction(moduleName, stopwatch.Elapsed.TotalMilliseconds, "Null response");
                }

                var prediction = new ModulePrediction
                {
                    ModuleName = moduleName,
                    Prediction = ParseDiagnosisType(result.Prediction),
                    Confidence = result.Confidence,
                    Probabilities = result.Probabilities ?? new Dictionary<string, double>(),
                    ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                    ExplainabilityData = result.Explainability,
                    Success = true
                };

                _logger.LogInformation(
                    "{Module} prediction: {Diagnosis} ({Confidence:P2}) in {Time}ms",
                    moduleName,
                    prediction.Prediction,
                    prediction.Confidence,
                    prediction.ProcessingTimeMs);

                return prediction;
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "{Module} module HTTP request failed", moduleName);
                return CreateErrorPrediction(moduleName, stopwatch.Elapsed.TotalMilliseconds, $"HTTP error: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "{Module} module request timed out", moduleName);
                return CreateErrorPrediction(moduleName, stopwatch.Elapsed.TotalMilliseconds, "Request timeout");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "{Module} module call failed unexpectedly", moduleName);
                return CreateErrorPrediction(moduleName, stopwatch.Elapsed.TotalMilliseconds, $"Unexpected error: {ex.Message}");
            }
        }

        private ModulePrediction CreateErrorPrediction(string moduleName, double processingTimeMs, string errorMessage)
        {
            return new ModulePrediction
            {
                ModuleName = moduleName,
                Prediction = DiagnosisType.Inconclusive,
                Confidence = 0.0,
                Probabilities = new Dictionary<string, double>(),
                ProcessingTimeMs = processingTimeMs,
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        private DiagnosisType ParseDiagnosisType(string prediction)
        {
            return prediction?.ToLower() switch
            {
                "benign" => DiagnosisType.Benign,
                "malignant" => DiagnosisType.Malignant,
                _ => DiagnosisType.Inconclusive
            };
        }

        /// <summary>
        /// Expected response format from Python AI modules
        /// </summary>
        private class ModuleResponse
        {
            public string Prediction { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public Dictionary<string, double>? Probabilities { get; set; }
            public Dictionary<string, object>? Explainability { get; set; }
        }
    }
}
