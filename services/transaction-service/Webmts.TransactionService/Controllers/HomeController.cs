using Microsoft.AspNetCore.Mvc;

namespace Webmts.TransactionService.Controllers;

[ApiController]
[Route("api/transaction")]
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
        _logger.LogInformation("Transaction service status check at {Time}", DateTime.UtcNow);
        
        return Ok(new
        {
            Service = "Transaction Service",
            Status = "Running",
            Timestamp = DateTime.UtcNow
        });
    }
}