using DiagnosisOrchestrator.Models;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosisOrchestrator.Services
{
    /// <summary>
    /// Repository interface for diagnosis data
    /// </summary>
    public interface IDiagnosisRepository
    {
        Task SaveDiagnosisAsync(UnifiedDiagnosis diagnosis, CancellationToken cancellationToken = default);
        Task<UnifiedDiagnosis?> GetDiagnosisByIdAsync(Guid diagnosisCaseId, CancellationToken cancellationToken = default);
        Task SaveModulePredictionAsync(Guid diagnosisCaseId, ModulePrediction prediction, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// TimescaleDB repository for storing diagnosis results
    /// </summary>
    public class DiagnosisRepository : IDiagnosisRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DiagnosisRepository> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public DiagnosisRepository(
            string connectionString,
            ILogger<DiagnosisRepository> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task SaveDiagnosisAsync(
            UnifiedDiagnosis diagnosis,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                const string sql = @"
                    INSERT INTO unified_diagnosis_results (
                        diagnosis_case_id,
                        final_diagnosis,
                        overall_confidence,
                        ensemble_probabilities,
                        contributing_modules,
                        risk_level,
                        recommendations,
                        explainability_summary,
                        status,
                        total_processing_time_ms,
                        error_details,
                        created_at
                    ) VALUES (
                        @diagnosisCaseId,
                        @finalDiagnosis,
                        @overallConfidence,
                        @ensembleProbabilities::jsonb,
                        @contributingModules,
                        @riskLevel,
                        @recommendations,
                        @explainabilitySummary::jsonb,
                        @status,
                        @totalProcessingTimeMs,
                        @errorDetails,
                        @createdAt
                    )
                    ON CONFLICT (diagnosis_case_id) 
                    DO UPDATE SET
                        final_diagnosis = EXCLUDED.final_diagnosis,
                        overall_confidence = EXCLUDED.overall_confidence,
                        ensemble_probabilities = EXCLUDED.ensemble_probabilities,
                        contributing_modules = EXCLUDED.contributing_modules,
                        risk_level = EXCLUDED.risk_level,
                        recommendations = EXCLUDED.recommendations,
                        explainability_summary = EXCLUDED.explainability_summary,
                        status = EXCLUDED.status,
                        total_processing_time_ms = EXCLUDED.total_processing_time_ms,
                        error_details = EXCLUDED.error_details,
                        updated_at = CURRENT_TIMESTAMP";

                await using var command = new NpgsqlCommand(sql, connection);
                
                command.Parameters.AddWithValue("diagnosisCaseId", diagnosis.DiagnosisCaseId);
                command.Parameters.AddWithValue("finalDiagnosis", diagnosis.FinalDiagnosis.ToString().ToLower());
                command.Parameters.AddWithValue("overallConfidence", diagnosis.OverallConfidence);
                command.Parameters.AddWithValue("ensembleProbabilities", JsonSerializer.Serialize(diagnosis.EnsembleProbabilities, _jsonOptions));
                command.Parameters.AddWithValue("contributingModules", diagnosis.ContributingModules.ToArray());
                command.Parameters.AddWithValue("riskLevel", diagnosis.RiskLevel.ToString().ToLower());
                command.Parameters.AddWithValue("recommendations", diagnosis.Recommendations.ToArray());
                command.Parameters.AddWithValue("explainabilitySummary", JsonSerializer.Serialize(diagnosis.ExplainabilitySummary, _jsonOptions));
                command.Parameters.AddWithValue("status", diagnosis.Status.ToString().ToLower());
                command.Parameters.AddWithValue("totalProcessingTimeMs", diagnosis.TotalProcessingTimeMs);
                command.Parameters.AddWithValue("errorDetails", (object?)diagnosis.ErrorDetails ?? DBNull.Value);
                command.Parameters.AddWithValue("createdAt", diagnosis.Timestamp);

                await command.ExecuteNonQueryAsync(cancellationToken);

                // Save individual module predictions
                foreach (var prediction in diagnosis.ModulePredictions)
                {
                    await SaveModulePredictionAsync(diagnosis.DiagnosisCaseId, prediction, cancellationToken);
                }

                _logger.LogInformation(
                    "Saved diagnosis result for case {CaseId} with status {Status}",
                    diagnosis.DiagnosisCaseId,
                    diagnosis.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save diagnosis for case {CaseId}", diagnosis.DiagnosisCaseId);
                throw;
            }
        }

        public async Task<UnifiedDiagnosis?> GetDiagnosisByIdAsync(
            Guid diagnosisCaseId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                const string sql = @"
                    SELECT 
                        diagnosis_case_id,
                        final_diagnosis,
                        overall_confidence,
                        ensemble_probabilities,
                        contributing_modules,
                        risk_level,
                        recommendations,
                        explainability_summary,
                        status,
                        total_processing_time_ms,
                        error_details,
                        created_at
                    FROM unified_diagnosis_results
                    WHERE diagnosis_case_id = @diagnosisCaseId";

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("diagnosisCaseId", diagnosisCaseId);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                var diagnosis = new UnifiedDiagnosis
                {
                    DiagnosisCaseId = reader.GetGuid(0),
                    FinalDiagnosis = Enum.Parse<DiagnosisType>(reader.GetString(1), true),
                    OverallConfidence = reader.GetDouble(2),
                    EnsembleProbabilities = JsonSerializer.Deserialize<Dictionary<string, double>>(
                        reader.GetString(3), _jsonOptions) ?? new Dictionary<string, double>(),
                    ContributingModules = new List<string>((string[])reader.GetValue(4)),
                    RiskLevel = Enum.Parse<RiskLevel>(reader.GetString(5), true),
                    Recommendations = new List<string>((string[])reader.GetValue(6)),
                    ExplainabilitySummary = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        reader.GetString(7), _jsonOptions) ?? new Dictionary<string, object>(),
                    Status = Enum.Parse<OrchestrationStatus>(reader.GetString(8), true),
                    TotalProcessingTimeMs = reader.GetDouble(9),
                    ErrorDetails = reader.IsDBNull(10) ? null : reader.GetString(10),
                    Timestamp = reader.GetDateTime(11),
                    ModulePredictions = new List<ModulePrediction>() // Will be loaded separately if needed
                };

                return diagnosis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve diagnosis for case {CaseId}", diagnosisCaseId);
                return null;
            }
        }

        public async Task SaveModulePredictionAsync(
            Guid diagnosisCaseId,
            ModulePrediction prediction,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                string tableName = prediction.ModuleName.ToLower() switch
                {
                    "imaging" => "imaging_results",
                    "clinical" => "clinical_results",
                    "laboratory" => "laboratory_results",
                    _ => throw new ArgumentException($"Unknown module name: {prediction.ModuleName}")
                };

                string sql = $@"
                    INSERT INTO {tableName} (
                        diagnosis_case_id,
                        prediction,
                        confidence,
                        probabilities,
                        processing_time_ms,
                        explainability_data,
                        success,
                        error_message,
                        created_at
                    ) VALUES (
                        @diagnosisCaseId,
                        @prediction,
                        @confidence,
                        @probabilities::jsonb,
                        @processingTimeMs,
                        @explainabilityData::jsonb,
                        @success,
                        @errorMessage,
                        @createdAt
                    )";

                await using var command = new NpgsqlCommand(sql, connection);
                
                command.Parameters.AddWithValue("diagnosisCaseId", diagnosisCaseId);
                command.Parameters.AddWithValue("prediction", prediction.Prediction.ToString().ToLower());
                command.Parameters.AddWithValue("confidence", prediction.Confidence);
                command.Parameters.AddWithValue("probabilities", JsonSerializer.Serialize(prediction.Probabilities, _jsonOptions));
                command.Parameters.AddWithValue("processingTimeMs", prediction.ProcessingTimeMs);
                command.Parameters.AddWithValue("explainabilityData", 
                    prediction.ExplainabilityData != null 
                        ? JsonSerializer.Serialize(prediction.ExplainabilityData, _jsonOptions) 
                        : (object)DBNull.Value);
                command.Parameters.AddWithValue("success", prediction.Success);
                command.Parameters.AddWithValue("errorMessage", (object?)prediction.ErrorMessage ?? DBNull.Value);
                command.Parameters.AddWithValue("createdAt", DateTime.UtcNow);

                await command.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogDebug(
                    "Saved {Module} prediction for case {CaseId}",
                    prediction.ModuleName,
                    diagnosisCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save {Module} prediction for case {CaseId}",
                    prediction.ModuleName,
                    diagnosisCaseId);
                // Don't throw - this is not critical
            }
        }
    }
}
