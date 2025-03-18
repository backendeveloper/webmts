using CustomerService.Contract.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.Client.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(IMediator mediator, ILogger<CustomerController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var request = new GetCustomersRequest
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
        _logger.LogInformation("Getting customer with ID: {CustomerId}", id);

        var request = new GetCustomerByIdRequest
        {
            CustomerId = id
        };

        var response = await _mediator.Send(request);

        return response.Success
            ? Ok(response)
            : response.Message?.Contains("not found") == true
                ? NotFound(new { Message = response.Message })
                : BadRequest(new { Message = response.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request)
    {
        _logger.LogInformation("Creating new customer with username: {Username}", request.Username);

        var response = await _mediator.Send(request);

        return response.Success
            ? CreatedAtAction(nameof(GetById), new { id = response }, response)
            : BadRequest(new { Message = response.Message });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        _logger.LogInformation("Updating customer with ID: {CustomerId}", id);

        request.CustomerId = id;

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
        _logger.LogInformation("Deleting customer with ID: {CustomerId}", id);

        var request = new DeleteCustomerRequest
        {
            CustomerId = id
        };

        var response = await _mediator.Send(request);

        return response.Success
            ? Ok(response)
            : response.Message?.Contains("not found") == true
                ? NotFound(new { Message = response.Message })
                : BadRequest(new { Message = response.Message });
    }
}