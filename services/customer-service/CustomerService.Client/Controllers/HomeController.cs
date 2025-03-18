using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Client.Controllers;

[ApiController]
[Route("api/customers")]
public class HomeController : ControllerBase
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
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

    [HttpGet("config-test")]
    public IActionResult GetConfigTest()
    {
        var testConfig = _configuration["TestConfig:Value"] ?? "Yapılandırma bulunamadı";
        var testConfigSource = _configuration.GetSection("TestConfig").Value ?? "Debug bilgisi yok";

        _logger.LogInformation("Config Test çağrıldı, şu anki değer: {TestConfig}", testConfig);

        return Ok(new
        {
            Service = "Auth Service",
            TestConfig = testConfig,
            ConfigSource = testConfigSource,
            Timestamp = DateTime.UtcNow
        });
    }
}