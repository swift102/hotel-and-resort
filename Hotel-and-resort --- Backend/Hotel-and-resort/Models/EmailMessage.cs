using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hotel_and_resort.Models;


public class EmailSender : IEmailSender
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<SmtpSettings> smtpSettings, ILogger<EmailSender> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            using var smtpClient = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port)
            {
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                EnableSsl = _smtpSettings.EnableSsl,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.From),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(email);
            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent to {Email}", email);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error occurred while sending email to {Email}: {Message}", email, smtpEx.Message);
            throw new InvalidOperationException("SMTP error occurred while sending email.", smtpEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending email to {Email}: {Message}", email, ex.Message);
            throw new InvalidOperationException("An error occurred while sending email.", ex);
        }
    }

    public Task<object> SendEmailAsync(EmailMessage emailMessage)
    {
        throw new NotImplementedException();
    }

    public async Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachment, string attachmentFileName)
    {
        try
        {
            using var smtpClient = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port)
            {
                Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                EnableSsl = _smtpSettings.EnableSsl,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpSettings.From),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(email);
            mailMessage.Attachments.Add(new Attachment(new MemoryStream(attachment), attachmentFileName));

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent to {Email} with attachment {AttachmentFileName}", email, attachmentFileName);
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError(smtpEx, "SMTP error occurred while sending email to {Email} with attachment {AttachmentFileName}: {Message}", email, attachmentFileName, smtpEx.Message);
            throw new InvalidOperationException("SMTP error occurred while sending email with attachment.", smtpEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending email to {Email} with attachment {AttachmentFileName}: {Message}", email, attachmentFileName, ex.Message);
            throw new InvalidOperationException("An error occurred while sending email with attachment.", ex);
        }
    }

}
public class EmailMessage
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Content { get; set; }
}

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
    Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachment, string attachmentFileName);
}