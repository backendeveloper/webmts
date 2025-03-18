using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Business.Senders.Interfaces;
using NotificationService.Contract.Enums;
using NotificationService.Data.Entities;

namespace NotificationService.Business.Senders;

public class SmsSender : INotificationSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(IConfiguration configuration, ILogger<SmsSender> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendAsync(Notification notification)
    {
        try
        {
            _logger.LogInformation($"Sending SMS to {notification.RecipientInfo}");
                
            var smsApiUrl = _configuration["SmsSettings:ApiUrl"];
            var smsApiKey = _configuration["SmsSettings:ApiKey"];
            var smsApiSecret = _configuration["SmsSettings:ApiSecret"];
            var smsSenderId = _configuration["SmsSettings:SenderId"];

            _logger.LogInformation($"SMS would be sent to {notification.RecipientInfo} with content: {notification.Content}");
                
            await Task.Delay(500);
                
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending SMS to {notification.RecipientInfo}");
            return false;
        }
    }

    public bool CanHandle(NotificationType type)
    {
        return type == NotificationType.SMS;
    }
}