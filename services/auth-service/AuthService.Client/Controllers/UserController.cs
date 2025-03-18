using AuthService.Contract.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Client.Controllers;

[ApiController]
[Route("api/auth/users")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UserController> _logger;

    public UserController(IMediator mediator, ILogger<UserController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Getting all users");

        var request = new GetUsersRequest
        {
            Page = page,
            PageSize = pageSize
        };

        var response = await _mediator.Send(request);

        return response.Success
            ? Ok(response)
            : BadRequest(new { Message = response.Message });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", id);

        var request = new GetUserByIdRequest
        {
            UserId = id
        };

        var response = await _mediator.Send(request);

        return response.Success
            ? Ok(response)
            : response.Message?.Contains("not found") == true
                ? NotFound(new { Message = response.Message })
                : BadRequest(new { Message = response.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        _logger.LogInformation("Creating new user with username: {Username}", request.Username);

        var response = await _mediator.Send(request);

        return response.Success
            ? CreatedAtAction(nameof(GetById), new { id = response.User.Id }, response)
            : BadRequest(new { Message = response.Message });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", id);

        request.UserId = id;

        var response = await _mediator.Send(request);

        return response.Success
            ? Ok(response)
            : response.Message?.Contains("not found") == true
                ? NotFound(new { Message = response.Message })
                : BadRequest(new { Message = response.Message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", id);

        var request = new DeleteUserRequest
        {
            UserId = id
        };

        var response = await _mediator.Send(request);

        return response.Success
            ? Ok(response)
            : response.Message?.Contains("not found") == true
                ? NotFound(new { Message = response.Message })
                : BadRequest(new { Message = response.Message });
    }
}