using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Contract.Requests;
using NotificationService.Contract.Responses;

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

    [HttpPost("send")]
    public async Task<ActionResult<SendNotificationResponse>> SendNotification([FromBody] SendNotificationRequest request)
    {
        _logger.LogInformation("Received request to send notification of type {NotificationType} to {RecipientId}", 
            request.Type, request.RecipientId);
        
        var response = await _mediator.Send(request);
        if (response.Success)
        {
            _logger.LogInformation("Successfully created notification {NotificationId}", response.Notification.Id);
            return Ok(response);
        }
        
        _logger.LogWarning("Failed to create notification: {ErrorMessage}", response.Message);
        return BadRequest(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<GetNotificationByIdResponse>> GetNotificationById(Guid id)
    {
        _logger.LogInformation("Received request to get notification {NotificationId}", id);
        
        var request = new GetNotificationByIdRequest { NotificationId = id };
        var response = await _mediator.Send(request);
        if (response.Success)
        {
            _logger.LogInformation("Successfully retrieved notification {NotificationId}", id);
            return Ok(response);
        }
        
        if (response.Message.Contains("not found"))
        {
            return NotFound(response);
        }
        
        _logger.LogWarning("Failed to retrieve notification {NotificationId}: {ErrorMessage}", id, response.Message);
        return BadRequest(response);
    }

    [HttpGet("all")]
    public async Task<ActionResult<GetNotificationsListResponse>> GetNotifications(
        [FromQuery] GetNotificationsListRequest request)
    {
        _logger.LogInformation("Received request to list notifications, page {PageNumber}, size {PageSize}", 
            request.PageNumber, request.PageSize);
        
        var response = await _mediator.Send(request);
        if (response.Success)
        {
            _logger.LogInformation("Successfully retrieved {Count} notifications", response.Notifications.Count);
            return Ok(response);
        }
        
        _logger.LogWarning("Failed to retrieve notifications: {ErrorMessage}", response.Message);
        return BadRequest(response);
    }
}