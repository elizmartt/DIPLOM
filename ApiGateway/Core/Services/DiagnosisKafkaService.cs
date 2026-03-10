using System.Text;
using System.Text.Json;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data.Repositories.Interfaces;

namespace ApiGateway.Core.Services
{
    public class DiagnosisKafkaService
    {
        private readonly HttpClient _httpClient;
        private readonly IMedicalImageRepository _imageRepository;
        private readonly IClinicalSymptomRepository _symptomRepository;
        private readonly ILabTestRepository _labTestRepository;
        private readonly ILogger<DiagnosisKafkaService> _logger;
        private readonly string _orchestratorBaseUrl;

        public DiagnosisKafkaService(
            HttpClient httpClient,
            IMedicalImageRepository imageRepository,
            IClinicalSymptomRepository symptomRepository,
            ILabTestRepository labTestRepository,
            IConfiguration configuration,
            ILogger<DiagnosisKafkaService> logger)
        {
            _httpClient = httpClient;
            _imageRepository = imageRepository;
            _symptomRepository = symptomRepository;
            _labTestRepository = labTestRepository;
            _logger = logger;
            _orchestratorBaseUrl = configuration["Orchestrator:BaseUrl"] ?? "http://localhost:5217";
        }

        public async Task<bool> SendDiagnosisRequestAsync(
            Guid caseId,
            Guid patientId,
            string diagnosisType,
            bool includeImaging,
            bool includeClinical,
            bool includeLaboratory)
        {
            try
            {
                
                var diagnosisRequest = new
                {
                    diagnosisCaseId = caseId,
                    patientId = patientId,
                    imagingData = includeImaging ? await GetImagingDataAsync(caseId) : null,
                    clinicalData = includeClinical ? await GetClinicalDataAsync(caseId) : null,
                    laboratoryData = includeLaboratory ? await GetLaboratoryDataAsync(caseId) : null
                };

                
                var json = JsonSerializer.Serialize(diagnosisRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                
                var endpoint = diagnosisType.ToLower() switch
                {
                    "brain_tumor" => $"{_orchestratorBaseUrl}/api/diagnosis/brain/sync",
                    "lung_cancer" => $"{_orchestratorBaseUrl}/api/diagnosis/lung/sync",
                    _ => $"{_orchestratorBaseUrl}/api/diagnosis/brain/sync" 
                };

                _logger.LogInformation(
                    "Sending diagnosis request to Orchestrator: {Endpoint} for case: {CaseId}",
                    endpoint, caseId);

                
                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(
                        "Diagnosis request sent successfully for case: {CaseId}. Response: {Result}",
                        caseId, result);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Failed to send diagnosis request. Status: {Status}, Error: {Error}",
                        response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception sending diagnosis request to Orchestrator for case: {CaseId}", caseId);
                return false;
            }
        }
  
        public async Task<bool> SendImageAnalysisRequestAsync(
            Guid caseId,
            Guid imageId,
            string imagePath,
            string imageType,
            string scanArea)
        {
            try
            {
                
                var analysisRequest = new
                {
                    diagnosisCaseId = caseId,
                    imageId = imageId,
                    imagePath = imagePath,
                    imageType = imageType,
                    scanArea = scanArea,
                    timestamp = DateTime.UtcNow
                };

                
                var json = JsonSerializer.Serialize(analysisRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

               
                var endpoint = $"{_orchestratorBaseUrl}/api/diagnosis/image-analysis";

                _logger.LogInformation(
                    "Sending image analysis request to: {Endpoint} for image: {ImageId}",
                    endpoint, imageId);

                var response = await _httpClient.PostAsync(endpoint, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Image analysis request sent successfully for image: {ImageId}", imageId);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Failed to send image analysis request. Status: {Status}, Error: {Error}",
                        response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception sending image analysis request for image: {ImageId}", imageId);
                return false;
            }
        }
        private async Task<Dictionary<string, object>?> GetImagingDataAsync(Guid caseId)
        {
            try
            {
                var images = await _imageRepository.GetByCaseIdAsync(caseId);
                var imagesList = images.ToList();

                if (!imagesList.Any())
                {
                    _logger.LogWarning("No images found for case: {CaseId}", caseId);
                    return null;
                }

                
                var image = imagesList.First();

               return new Dictionary<string, object>
                {
                    { "image_id", image.ImageId.ToString() },
                    { "image_type", image.ImageType },
                    { "scan_area", image.ScanArea },
                    { "file_path", image.FilePath }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting imaging data for case: {CaseId}", caseId);
                return null;
            }
        }

        private async Task<Dictionary<string, object>?> GetClinicalDataAsync(Guid caseId)
        {
            try
            {
                var symptom = await _symptomRepository.GetByCaseIdAsync(caseId);

                if (symptom == null)
                {
                    _logger.LogWarning("No clinical symptoms found for case: {CaseId}", caseId);
                    return null;
                }

                var symptomsData = new Dictionary<string, object>();

                var symptomsList = JsonSerializer.Deserialize<List<string>>(symptom.Symptoms);
                if (symptomsList != null)
                {
                    foreach (var s in symptomsList)
                    {
                        symptomsData[s] = true;
                    }
                }

                if (!string.IsNullOrEmpty(symptom.BloodPressure))
                    symptomsData["blood_pressure"] = symptom.BloodPressure;

                if (symptom.HeartRate.HasValue)
                    symptomsData["heart_rate"] = symptom.HeartRate.Value;

                if (symptom.Temperature.HasValue)
                    symptomsData["temperature"] = symptom.Temperature.Value;

                if (symptom.SmokingHistory.HasValue)
                    symptomsData["smoking_history"] = symptom.SmokingHistory.Value;


                if (!string.IsNullOrEmpty(symptom.FamilyHistory) && symptom.FamilyHistory != "{}")
                {
                    try
                    {
                        var historyDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(symptom.FamilyHistory);
                        if (historyDict != null)
                        {
                            foreach (var kvp in historyDict)
                            {
                                symptomsData[kvp.Key] = kvp.Value.ValueKind switch
                                {
                                    JsonValueKind.Number  => kvp.Value.TryGetDouble(out var d) ? (object)d : (object)kvp.Value.GetInt32(),
                                    JsonValueKind.True    => (object)true,
                                    JsonValueKind.False   => (object)false,
                                    JsonValueKind.String  => (object)(kvp.Value.GetString() ?? ""),
                                    _                     => (object)kvp.Value.GetRawText()
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to merge FamilyHistory for case {CaseId}", caseId);
                    }
                }

                return symptomsData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting clinical data for case: {CaseId}", caseId);
                return null;
            }
        }

        private async Task<Dictionary<string, object>?> GetLaboratoryDataAsync(Guid caseId)
        {
            try
            {
                var labTests = await _labTestRepository.GetByCaseIdAsync(caseId);
                var labTestsList = labTests.ToList();

                if (!labTestsList.Any())
                {
                    _logger.LogWarning("No lab tests found for case: {CaseId}", caseId);
                    return null;
                }

                var latestTest = labTestsList.OrderByDescending(l => l.TestDate).First();

                var testResults = JsonSerializer.Deserialize<Dictionary<string, object>>(latestTest.TestResults);
                return testResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting laboratory data for case: {CaseId}", caseId);
                return null;
            }
        }
    }
}