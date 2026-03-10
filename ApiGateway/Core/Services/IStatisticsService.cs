
using MedicalDiagnosticSystem.DiagnosisOrchestrator.Models.Statistics;

namespace ApiGateway.Core.Services;
public interface IStatisticsService
{
    Task<StatisticsOverviewDto> GetOverviewAsync();
    Task<List<DiagnosisDistributionDto>> GetDiagnosisDistributionAsync();
    Task<ModelAccuracyDto> GetModelAccuracyAsync();
    Task<List<ConfidenceTrendDto>> GetConfidenceTrendsAsync();
}