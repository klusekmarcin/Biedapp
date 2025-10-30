using Biedapp.Application.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace Biedapp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SystemController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly ILogger<SystemController> _logger;
    private readonly IConfiguration _configuration;

    public SystemController(
        IBudgetService budgetService,
        ILogger<SystemController> logger,
        IConfiguration configuration)
    {
        _budgetService = budgetService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get system information
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemInfo()
    {
        try
        {
            string storageLocation = _budgetService.GetStorageLocation();
            int eventCount = await _budgetService.GetEventCountAsync();
            string storageType = _configuration["Storage:Type"] ?? "File";

            var info = new
            {
                application = "BiedApp",
                version = "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                storage = new
                {
                    type = storageType,
                    location = storageLocation,
                    eventCount = eventCount,
                    isEncrypted = storageType.Equals("File", StringComparison.OrdinalIgnoreCase)
                },
                system = new
                {
                    machineName = Environment.MachineName,
                    userName = Environment.UserName,
                    osVersion = Environment.OSVersion.ToString(),
                    appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                }
            };

            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system info");
            return StatusCode(500, new { error = "Failed to retrieve system info" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }
}