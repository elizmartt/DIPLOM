using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiGateway.Core.Services;

namespace ApiGateway.Adapters.Inbound.Http.Controllers;

[ApiController]
[Route("api/cases")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportGeneratorService _reportService;

    public ReportController(IReportGeneratorService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("{id}/report")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        try
        {
            var pdfBytes = await _reportService.GenerateCaseReportAsync(id);
            return File(pdfBytes, "application/pdf", $"diagnosis-report-{id:N}.pdf");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}