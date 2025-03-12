using Microsoft.AspNetCore.Mvc;

namespace Webmts.NotificationService.Controllers;

[ApiController]
[Route("api/notification")]
public class HomeController : ControllerBase
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        _logger.LogInformation("Notification service status check at {Time}", DateTime.UtcNow);
        
        return Ok(new
        {
            Service = "Notification Service",
            Status = "Running",
            Timestamp = DateTime.UtcNow
        });
    }
}