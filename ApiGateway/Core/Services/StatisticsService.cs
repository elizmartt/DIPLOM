using MedicalDiagnosticSystem.DiagnosisOrchestrator.Models.Statistics;
using MedicalDiagnosticSystem.MedicalDiagnostic.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiGateway.Core.Services;

public class StatisticsService : IStatisticsService
{
    private readonly MedicalDiagnosticDbContext _db;

    public StatisticsService(MedicalDiagnosticDbContext db)
    {
        _db = db;
    }

    public async Task<StatisticsOverviewDto> GetOverviewAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var twelveMonthsAgo = now.AddMonths(-12);

        var cases = await _db.DiagnosisCases.ToListAsync();

        var monthlyTrends = cases
            .Where(c => c.CreatedAt >= twelveMonthsAgo)
            .GroupBy(c => new DateTime(c.CreatedAt.Year, c.CreatedAt.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new MonthlyTrendDto
            {
                Month = g.Key.ToString("yyyy-MM"),
                Total = g.Count(),
                Brain = g.Count(c => c.DiagnosisType == "brain" || c.DiagnosisType == "brain_tumor"),
                Lung  = g.Count(c => c.DiagnosisType == "lung"  || c.DiagnosisType == "lung_cancer")
            })
            .ToList();

        return new StatisticsOverviewDto
        {
            TotalCases       = cases.Count,
            BrainCases       = cases.Count(c => c.DiagnosisType == "brain" || c.DiagnosisType == "brain_tumor"),
            LungCases        = cases.Count(c => c.DiagnosisType == "lung"  || c.DiagnosisType == "lung_cancer"),
            CompletedCases   = cases.Count(c => c.Status == "completed"),
            PendingCases     = cases.Count(c => c.Status == "pending"),
            AnalyzingCases   = cases.Count(c => c.Status == "analyzing" || c.Status == "data_collection"),
            CasesLast30Days  = cases.Count(c => c.CreatedAt >= thirtyDaysAgo),
            MonthlyTrends    = monthlyTrends
        };
    }

    public async Task<List<DiagnosisDistributionDto>> GetDiagnosisDistributionAsync()
    {
        var total = await _db.UnifiedDiagnosisResults
            .Where(r => r.Status == "completed")
            .CountAsync();

        if (total == 0) return [];

        return await _db.UnifiedDiagnosisResults
            .Where(r => r.Status == "completed")
            .GroupBy(r => r.FinalDiagnosis)
            .Select(g => new DiagnosisDistributionDto
            {
                Diagnosis         = g.Key,
                Count             = g.Count(),
                AverageConfidence = Math.Round(g.Average(r => r.OverallConfidence), 3),
                Percentage        = Math.Round(g.Count() * 100.0 / total, 1)
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
    }

    public async Task<ModelAccuracyDto> GetModelAccuracyAsync()
    {
        var liveStats = await _db.UnifiedDiagnosisResults
            .Where(r => r.Status == "completed" && r.TotalProcessingTimeMs != null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalProcessed  = g.Count(),
                AvgProcessingMs = g.Average(r => r.TotalProcessingTimeMs!.Value)
            })
            .FirstOrDefaultAsync();

        return new ModelAccuracyDto
        {
            Models =
            [
                new ModelStatsDto
                {
                    Name             = "Medical Imaging",
                    Architecture     = "ResNet18",
                    TrainedAccuracy  = 0.918,
                    Weighting        = 0.40
                },
                new ModelStatsDto
                {
                    Name             = "Laboratory Analysis",
                    Architecture     = "Random Forest",
                    TrainedAccuracy  = 0.880,
                    Weighting        = 0.30
                },
                new ModelStatsDto
                {
                    Name             = "Clinical Symptoms",
                    Architecture     = "Logistic Regression",
                    TrainedAccuracy  = 0.858,
                    Weighting        = 0.30
                },
                new ModelStatsDto
                {
                    Name             = "Weighted Ensemble",
                    Architecture     = "Fusion (40/30/30)",
                    TrainedAccuracy  = 0.942,
                    Weighting        = 1.0
                }
            ],
            LiveStats = new LiveProcessingStatsDto
            {
                TotalCasesProcessed    = liveStats?.TotalProcessed ?? 0,
                AverageProcessingTimeMs = Math.Round(liveStats?.AvgProcessingMs ?? 0, 2)
            }
        };
    }

    public async Task<List<ConfidenceTrendDto>> GetConfidenceTrendsAsync()
    {
        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);

        var data = await _db.UnifiedDiagnosisResults
            .Where(r => r.CreatedAt >= twelveMonthsAgo && r.Status == "completed")
            .Select(r => new { r.DiagnosisCaseId, r.CreatedAt, r.OverallConfidence })
            .ToListAsync();

        if (!data.Any()) return [];

        var imaging = await _db.ImagingResults
            .Where(r => r.CreatedAt >= twelveMonthsAgo)
            .Select(r => new { r.DiagnosisCaseId, r.Confidence })
            .ToListAsync();

        var lab = await _db.LaboratoryResults
            .Where(r => r.CreatedAt >= twelveMonthsAgo)
            .Select(r => new { r.DiagnosisCaseId, r.Confidence })
            .ToListAsync();

        var clinical = await _db.ClinicalResults
            .Where(r => r.CreatedAt >= twelveMonthsAgo)
            .Select(r => new { r.DiagnosisCaseId, r.Confidence })
            .ToListAsync();

        var imagingDict  = imaging.GroupBy(x => x.DiagnosisCaseId)
            .ToDictionary(g => g.Key, g => g.Average(x => x.Confidence));
        var labDict      = lab.GroupBy(x => x.DiagnosisCaseId)
            .ToDictionary(g => g.Key, g => g.Average(x => x.Confidence));
        var clinicalDict = clinical.GroupBy(x => x.DiagnosisCaseId)
            .ToDictionary(g => g.Key, g => g.Average(x => x.Confidence));

        return data
            .GroupBy(r => new DateTime(r.CreatedAt.Year, r.CreatedAt.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new ConfidenceTrendDto
            {
                Month      = g.Key.ToString("yyyy-MM"),
                Overall    = Math.Round(g.Average(r => r.OverallConfidence), 3),
                Imaging    = Math.Round(g
                    .Where(r => imagingDict.ContainsKey(r.DiagnosisCaseId))
                    .Select(r => imagingDict[r.DiagnosisCaseId])
                    .DefaultIfEmpty(0).Average(), 3),
                Laboratory = Math.Round(g
                    .Where(r => labDict.ContainsKey(r.DiagnosisCaseId))
                    .Select(r => labDict[r.DiagnosisCaseId])
                    .DefaultIfEmpty(0).Average(), 3),
                Clinical   = Math.Round(g
                    .Where(r => clinicalDict.ContainsKey(r.DiagnosisCaseId))
                    .Select(r => clinicalDict[r.DiagnosisCaseId])
                    .DefaultIfEmpty(0).Average(), 3)
            })
            .ToList();
    }
}