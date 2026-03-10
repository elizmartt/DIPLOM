namespace MedicalDiagnosticSystem.DiagnosisOrchestrator.Models.Statistics;

public record StatisticsOverviewDto
{
    public int TotalCases { get; init; }
    public int BrainCases { get; init; }
    public int LungCases { get; init; }
    public int CompletedCases { get; init; }
    public int PendingCases { get; init; }        // used by DoctorProfile
    public int AnalyzingCases { get; init; }      // used by DoctorProfile
    public int CasesLast30Days { get; init; }
    public List<MonthlyTrendDto> MonthlyTrends { get; init; } = [];
}

public record MonthlyTrendDto
{
    public string Month { get; init; } = "";      // "2024-01" format - frontend expects string
    public int Total { get; init; }
    public int Brain { get; init; }
    public int Lung { get; init; }
}

public record DiagnosisDistributionDto
{
    public string Diagnosis { get; init; } = "";
    public int Count { get; init; }
    public double AverageConfidence { get; init; }
    public double Percentage { get; init; }
}

public record ModelAccuracyDto
{
    public List<ModelStatsDto> Models { get; init; } = [];
    public LiveProcessingStatsDto LiveStats { get; init; } = new();
}

public record ModelStatsDto
{
    public string Name { get; init; } = "";
    public string Architecture { get; init; } = "";
    public double TrainedAccuracy { get; init; }
    public double Weighting { get; init; }
}

public record LiveProcessingStatsDto
{
    public int TotalCasesProcessed { get; init; }
    public double AverageProcessingTimeMs { get; init; }
}

public record ConfidenceTrendDto
{
    public string Month { get; init; } = "";      // "2024-01" format
    public double Overall { get; init; }
    public double Imaging { get; init; }
    public double Laboratory { get; init; }
    public double Clinical { get; init; }
}