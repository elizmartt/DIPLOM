namespace ApiGateway.Core.Services;

public interface IReportGeneratorService
{
    Task<byte[]> GenerateCaseReportAsync(Guid caseId);
}