using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Client.Controllers;

[ApiController]
[Route("api/notification")]
public class NotificationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(IMediator mediator, ILogger<NotificationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // [HttpPost]
    // [Authorize]
    // [ProducesResponseType(StatusCodes.Status201Created)]
    // [ProducesResponseType(StatusCodes.Status400BadRequest)]
    // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // public async Task<ActionResult<CreateTransactionResponse>> CreateTransaction([FromBody] CreateTransactionRequest request)
    // {
    //     _logger.LogInformation("Creating new transaction");
    //
    //     try
    //     {
    //         var response = await _mediator.Send(request);
    //
    //         if (!response.Success)
    //             return BadRequest(response);
    //
    //         return CreatedAtAction(nameof(CreateTransaction), new { id = response.Transaction.Id }, response);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error creating transaction");
    //         return StatusCode(StatusCodes.Status500InternalServerError, new CreateTransactionResponse
    //         {
    //             Success = false,
    //             Message = $"An error occurred: {ex.Message}"
    //         });
    //     }
    // }
    //
    // [HttpGet("{id}")]
    // [Authorize]
    // [ProducesResponseType(StatusCodes.Status200OK)]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // public async Task<ActionResult<GetTransactionByIdResponse>> GetTransactionById(Guid id)
    // {
    //     _logger.LogInformation($"Getting transaction with id {id}");
    //
    //     try
    //     {
    //         var query = new GetTransactionByIdRequest { TransactionId = id };
    //         var response = await _mediator.Send(query);
    //         if (!response.Success)
    //             return NotFound(response);
    //
    //         return Ok(response);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, $"Error getting transaction with id {id}");
    //         return StatusCode(StatusCodes.Status500InternalServerError, new GetTransactionByIdResponse
    //         {
    //             Success = false,
    //             Message = $"An error occurred: {ex.Message}"
    //         });
    //     }
    // }
    //
    // [HttpGet("all")]
    // [Authorize]
    // [ProducesResponseType(StatusCodes.Status200OK)]
    // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // public async Task<ActionResult<GetTransactionsListResponse>> GetTransactions([FromBody] GetTransactionsListRequest request)
    // {
    //     _logger.LogInformation("Getting transactions list");
    //
    //     try
    //     {
    //         var response = await _mediator.Send(request);
    //
    //         return Ok(response);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error getting transactions list");
    //         return StatusCode(StatusCodes.Status500InternalServerError, new GetTransactionsListResponse
    //         {
    //             Success = false,
    //             Message = $"An error occurred: {ex.Message}"
    //         });
    //     }
    // }
    //
    // [HttpPut("{id}/status")]
    // [Authorize]
    // [ProducesResponseType(StatusCodes.Status200OK)]
    // [ProducesResponseType(StatusCodes.Status400BadRequest)]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // public async Task<ActionResult<UpdateTransactionStatusResponse>> UpdateTransactionStatus(
    //     Guid id,
    //     [FromBody] UpdateTransactionStatusRequest request)
    // {
    //     _logger.LogInformation($"Updating status for transaction with id {id}");
    //
    //     if (id != request.TransactionId)
    //         return BadRequest(new UpdateTransactionStatusResponse
    //         {
    //             Success = false,
    //             Message = "Transaction ID in URL and body do not match"
    //         });
    //
    //     try
    //     {
    //         var response = await _mediator.Send(request);
    //
    //         if (!response.Success)
    //         {
    //             if (response.Message.Contains("not found"))
    //                 return NotFound(response);
    //
    //             return BadRequest(response);
    //         }
    //
    //         return Ok(response);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, $"Error updating status for transaction with id {id}");
    //         return StatusCode(StatusCodes.Status500InternalServerError, new UpdateTransactionStatusResponse
    //         {
    //             Success = false,
    //             Message = $"An error occurred: {ex.Message}"
    //         });
    //     }
    // }
    //
    // [HttpPost("{id}/cancel")]
    // [Authorize] 
    // [ProducesResponseType(StatusCodes.Status200OK)]
    // [ProducesResponseType(StatusCodes.Status400BadRequest)]
    // [ProducesResponseType(StatusCodes.Status404NotFound)]
    // [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    // public async Task<ActionResult<CancelTransactionResponse>> CancelTransaction(
    //     Guid id,
    //     [FromBody] CancelTransactionRequest request)
    // {
    //     _logger.LogInformation($"Cancelling transaction with id {id}");
    //
    //     if (id != request.TransactionId)
    //         return BadRequest(new CancelTransactionResponse
    //         {
    //             Success = false,
    //             Message = "Transaction ID in URL and body do not match"
    //         });
    //
    //     try
    //     {
    //         var response = await _mediator.Send(request);
    //
    //         if (!response.Success)
    //         {
    //             if (response.Message.Contains("not found"))
    //                 return NotFound(response);
    //
    //             return BadRequest(response);
    //         }
    //
    //         return Ok(response);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, $"Error cancelling transaction with id {id}");
    //         return StatusCode(StatusCodes.Status500InternalServerError, new CancelTransactionResponse
    //         {
    //             Success = false,
    //             Message = $"An error occurred: {ex.Message}"
    //         });
    //     }
    // }
}