using Microsoft.AspNetCore.Mvc;

namespace AuthService.Client.Controllers;

[ApiController]
[Route("api/auth")]
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
        _logger.LogInformation("Auth service status check at {Time}", DateTime.UtcNow);
        
        return Ok(new
        {
            Service = "Auth Service",
            Status = "Running",
            Timestamp = DateTime.UtcNow
        });
    }
}