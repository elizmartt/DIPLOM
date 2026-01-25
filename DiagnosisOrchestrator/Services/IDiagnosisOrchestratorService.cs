using DiagnosisOrchestrator.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DiagnosisOrchestrator.Services
{
    /// <summary>
    /// Service interface for orchestrating multi-modal diagnosis
    /// </summary>
    public interface IDiagnosisOrchestratorService
    {
        /// <summary>
        /// Orchestrates diagnosis from multiple AI modules
        /// </summary>
        /// <param name="request">Diagnosis request with patient data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Unified diagnosis with confidence scores</returns>
        Task<UnifiedDiagnosis> OrchestrateAsync(
            DiagnosisRequest request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Orchestrates diagnosis asynchronously via Kafka
        /// </summary>
        /// <param name="request">Diagnosis request</param>
        /// <returns>Task that completes when message is sent</returns>
        Task<Guid> QueueDiagnosisAsync(DiagnosisRequest request);

        /// <summary>
        /// Gets the status of an ongoing diagnosis
        /// </summary>
        /// <param name="diagnosisCaseId">The diagnosis case ID</param>
        /// <returns>Current status and partial results if available</returns>
        Task<UnifiedDiagnosis?> GetDiagnosisStatusAsync(Guid diagnosisCaseId);
    }
}
