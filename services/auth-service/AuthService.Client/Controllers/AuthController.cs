using AuthService.Common.Exceptions;
using AuthService.Contract.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Client.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Login attempt for user: {Username}", request.Username);
        
        try
        {
            var response = await _mediator.Send(request);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (BusinessValidationException ex)
        {
            _logger.LogWarning("Login validation failed: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.ValidationErrors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for user {Username}", request.Username);
            return StatusCode(500, new { Success = false, Message = "An internal server error occurred" });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("Registration attempt for username: {Username}", request.Username);
        
        try
        {
            var response = await _mediator.Send(request);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (BusinessValidationException ex)
        {
            _logger.LogWarning("Registration validation failed: {Message}", ex.Message);
            return BadRequest(new { Success = false, Message = ex.ValidationErrors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during registration for user {Username}", request.Username);
            return StatusCode(500, new { Success = false, Message = "An internal server error occurred" });
        }
    }

    [HttpPost("validate-token")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            var response = await _mediator.Send(request);
            return Ok(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new { Success = false, Message = ex.ValidationErrors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return StatusCode(500, new { Success = false, Message = "An internal server error occurred" });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _mediator.Send(request);
            return response.Success ? Ok(response) : BadRequest(response);
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new { Success = false, Message = ex.ValidationErrors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { Success = false, Message = "An internal server error occurred" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        try
        {
            await _mediator.Send(request);
            return Ok(new { Success = true, Message = "Logout successful" });
        }
        catch (BusinessValidationException ex)
        {
            return BadRequest(new { Success = false, Message = ex.ValidationErrors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { Success = false, Message = "An internal server error occurred" });
        }
    }
}