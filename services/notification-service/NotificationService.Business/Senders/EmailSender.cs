using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Business.Senders.Interfaces;
using NotificationService.Contract.Enums;
using NotificationService.Data.Entities;

namespace NotificationService.Business.Senders;

public class EmailSender : INotificationSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendAsync(Notification notification)
    {
        try
        {
            _logger.LogInformation(
                $"Sending email to {notification.RecipientInfo} with subject {notification.Subject}");

            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
            var smtpUsername = _configuration["EmailSettings:Username"];
            var smtpPassword = _configuration["EmailSettings:Password"];
            var smtpSenderEmail = _configuration["EmailSettings:SenderEmail"];
            var smtpSenderName = _configuration["EmailSettings:SenderName"];
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]);

            using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
            {
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = enableSsl;

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(smtpSenderEmail, smtpSenderName);
                    message.To.Add(new MailAddress(notification.RecipientInfo));
                    message.Subject = notification.Subject;
                    message.Body = notification.Content;
                    message.IsBodyHtml = true;

                    await smtpClient.SendMailAsync(message);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending email to {notification.RecipientInfo}");
            return false;
        }
    }

    public bool CanHandle(NotificationType type)
    {
        return type == NotificationType.Email;
    }
}