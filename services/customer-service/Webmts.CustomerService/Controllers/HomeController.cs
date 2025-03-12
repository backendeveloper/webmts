using Microsoft.AspNetCore.Mvc;

namespace Webmts.CustomerService.Controllers;

[ApiController]
[Route("api/customer")]
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
        _logger.LogInformation("Customer service status check at {Time}", DateTime.UtcNow);
        
        return Ok(new
        {
            Service = "Customer Service",
            Status = "Running",
            Timestamp = DateTime.UtcNow
        });
    }
}