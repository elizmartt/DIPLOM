using ApiGateway.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Adapters.Inbound.Http.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;

    public StatisticsController(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var result = await _statisticsService.GetOverviewAsync();
        return Ok(result);
    }

    [HttpGet("diagnoses")]
    public async Task<IActionResult> GetDiagnosisDistribution()
    {
        var result = await _statisticsService.GetDiagnosisDistributionAsync();
        return Ok(result);
    }

    [HttpGet("model-accuracy")]
    public async Task<IActionResult> GetModelAccuracy()
    {
        var result = await _statisticsService.GetModelAccuracyAsync();
        return Ok(result);
    }

    [HttpGet("confidence-trends")]
    public async Task<IActionResult> GetConfidenceTrends()
    {
        var result = await _statisticsService.GetConfidenceTrendsAsync();
        return Ok(result);
    }
}