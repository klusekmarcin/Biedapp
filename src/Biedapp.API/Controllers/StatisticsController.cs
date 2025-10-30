using Biedapp.Application.DTOs;
using Biedapp.Application.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace Biedapp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        IStatisticsService statisticsService,
        ILogger<StatisticsController> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get balance trend for the last 12 months
    /// </summary>
    [HttpGet("trends/balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalanceTrend()
    {
        try
        {
            Dictionary<string, decimal> trend = await _statisticsService.GetLast12MonthsBalanceAsync();
            return Ok(trend);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving balance trend");
            return StatusCode(500, new { error = "Failed to retrieve balance trend" });
        }
    }

    /// <summary>
    /// Get top expense categories
    /// </summary>
    [HttpGet("top-expenses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopExpenses([FromQuery] int top = 5)
    {
        try
        {
            if (top < 1 || top > 20)
                return BadRequest(new { error = "Top parameter must be between 1 and 20" });

            List<CategorySummaryDto> topExpenses = await _statisticsService.GetTopExpenseCategoriesAsync(top);
            return Ok(topExpenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top expenses");
            return StatusCode(500, new { error = "Failed to retrieve top expenses" });
        }
    }

    /// <summary>
    /// Get average monthly expenses
    /// </summary>
    [HttpGet("average-expenses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAverageExpenses([FromQuery] int months = 6)
    {
        try
        {
            if (months < 1 || months > 24)
                return BadRequest(new { error = "Months parameter must be between 1 and 24" });

            decimal average = await _statisticsService.GetAverageMonthlyExpensesAsync(months);

            return Ok(new
            {
                averageMonthlyExpenses = average,
                currency = "PLN",
                periodMonths = months
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving average expenses");
            return StatusCode(500, new { error = "Failed to retrieve average expenses" });
        }
    }
}
