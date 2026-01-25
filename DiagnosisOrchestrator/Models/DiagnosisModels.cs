using System;
using System.Collections.Generic;

namespace DiagnosisOrchestrator.Models
{
    /// <summary>
    /// Diagnosis classification types
    /// </summary>
    public enum DiagnosisType
    {
        Benign,
        Malignant,
        Inconclusive
    }

    /// <summary>
    /// Risk level assessment
    /// </summary>
    public enum RiskLevel
    {
        Low,
        Moderate,
        High,
        Critical
    }

    /// <summary>
    /// Status of the orchestration process
    /// </summary>
    public enum OrchestrationStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        PartialSuccess
    }

    /// <summary>
    /// Prediction from a single AI module
    /// </summary>
    public record ModulePrediction
    {
        public required string ModuleName { get; init; }
        public required DiagnosisType Prediction { get; init; }
        public required double Confidence { get; init; }
        public required Dictionary<string, double> Probabilities { get; init; }
        public required double ProcessingTimeMs { get; init; }
        public Dictionary<string, object>? ExplainabilityData { get; init; }
        public bool Success { get; init; } = true;
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Unified diagnosis result from orchestrator
    /// </summary>
    public record UnifiedDiagnosis
    {
        public required Guid DiagnosisCaseId { get; init; }
        public required DiagnosisType FinalDiagnosis { get; init; }
        public required double OverallConfidence { get; init; }
        public required List<ModulePrediction> ModulePredictions { get; init; }
        public required Dictionary<string, double> EnsembleProbabilities { get; init; }
        public required List<string> ContributingModules { get; init; }
        public required RiskLevel RiskLevel { get; init; }
        public required List<string> Recommendations { get; init; }
        public required Dictionary<string, object> ExplainabilitySummary { get; init; }
        public required OrchestrationStatus Status { get; init; }
        public required DateTime Timestamp { get; init; }
        public required double TotalProcessingTimeMs { get; init; }
        public string? ErrorDetails { get; init; }
    }

    /// <summary>
    /// Request to start diagnosis orchestration
    /// </summary>
    public record DiagnosisRequest
    {
        public required Guid DiagnosisCaseId { get; init; }
        public required Guid PatientId { get; init; }
        public required Guid DoctorId { get; init; }
        
        // Module-specific data
        public ImagingData? ImagingData { get; init; }
        public ClinicalData? ClinicalData { get; init; }
        public LaboratoryData? LaboratoryData { get; init; }
        
        public DateTime RequestTimestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Imaging module input data
    /// </summary>
    public record ImagingData
    {
        public required string ImagePath { get; init; }
        public required string ImagingType { get; init; } // CT, MRI, X-Ray
        public string? BodyRegion { get; init; }
        public Dictionary<string, object>? Metadata { get; init; }
    }

    /// <summary>
    /// Clinical symptoms module input data
    /// </summary>
    public record ClinicalData
    {
        public required Dictionary<string, object> Symptoms { get; init; }
        public int? Age { get; init; }
        public string? Gender { get; init; }
        public List<string>? MedicalHistory { get; init; }
        public Dictionary<string, object>? VitalSigns { get; init; }
    }

    /// <summary>
    /// Laboratory results module input data
    /// </summary>
    public record LaboratoryData
    {
        public required Dictionary<string, double> TumorMarkers { get; init; }
        public Dictionary<string, double>? BloodWork { get; init; }
        public Dictionary<string, object>? AdditionalTests { get; init; }
    }

    /// <summary>
    /// Module weights configuration
    /// </summary>
    public record ModuleWeights
    {
        public double Imaging { get; init; } = 0.40;
        public double Clinical { get; init; } = 0.30;
        public double Laboratory { get; init; } = 0.30;

        /// <summary>
        /// Returns normalized weights that sum to 1.0
        /// </summary>
        public ModuleWeights Normalize()
        {
            var total = Imaging + Clinical + Laboratory;
            return new ModuleWeights
            {
                Imaging = Imaging / total,
                Clinical = Clinical / total,
                Laboratory = Laboratory / total
            };
        }
    }

    /// <summary>
    /// Orchestrator configuration options
    /// </summary>
    public class OrchestratorOptions
    {
        public ModuleWeights ModuleWeights { get; set; } = new();
        public double ConfidenceThreshold { get; set; } = 0.70;
        public int MinModulesRequired { get; set; } = 2;
        public int TimeoutSeconds { get; set; } = 30;
        public bool EnableParallelProcessing { get; set; } = true;
        public string EnsembleStrategy { get; set; } = "WeightedVoting";
    }

    /// <summary>
    /// AI Module service endpoints configuration
    /// </summary>
    public class ModuleEndpoints
    {
        public required string ImagingServiceUrl { get; set; }
        public required string ClinicalServiceUrl { get; set; }
        public required string LaboratoryServiceUrl { get; set; }
    }
}
