using Ganss.Xss;
using hotel_and_resort.Models;
using Hotel_and_resort.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace hotel_and_resort.Services
{
    public class EmailSender : IEmailSender, Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailSender> _logger;
        private readonly IHtmlSanitizer _sanitizer;

        public EmailSender(IOptions<SmtpSettings> smtpSettings, ILogger<EmailSender> logger)
        {
            _smtpSettings = smtpSettings.Value ?? throw new ArgumentNullException(nameof(smtpSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.Add("p");
            _sanitizer.AllowedTags.Add("br");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("em");
            _sanitizer.AllowedTags.Add("h3");
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailMessage = new EmailMessage
            {
                To = email,
                Subject = subject,
                Body = htmlMessage
            };
            await SendEmailAsync(emailMessage);
        }

        public async Task SendEmailAsync(EmailMessage emailMessage)
        {
            if (emailMessage == null)
            {
                _logger.LogWarning("Attempted to send null email message.");
                throw new ArgumentNullException(nameof(emailMessage));
            }

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(emailMessage.To) || !IsValidEmail(emailMessage.To))
                {
                    _logger.LogWarning("Invalid email address: {To}", emailMessage.To);
                    throw new EmailValidationException("Invalid or missing recipient email address.");
                }
                if (string.IsNullOrWhiteSpace(emailMessage.Subject))
                {
                    _logger.LogWarning("Missing email subject.");
                    throw new EmailValidationException("Email subject is required.");
                }
                if (string.IsNullOrWhiteSpace(emailMessage.Body))
                {
                    _logger.LogWarning("Missing email body.");
                    throw new EmailValidationException("Email body is required.");
                }

                // Validate SMTP settings
                if (string.IsNullOrEmpty(_smtpSettings.Server) || string.IsNullOrEmpty(_smtpSettings.From))
                {
                    _logger.LogError("SMTP configuration missing (Server or From address).");
                    throw new EmailServiceException("SMTP configuration is incomplete.");
                }

                using var smtpClient = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    EnableSsl = _smtpSettings.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.From),
                    Subject = emailMessage.Subject,
                    Body = emailMessage.Body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(emailMessage.To);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {To}", emailMessage.To);

                await PublishEmailSentEvent(emailMessage);
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending email to {To}", emailMessage.To);
                throw new EmailServiceException("Failed to send email due to SMTP error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {To}", emailMessage.To);
                throw new EmailServiceException("Failed to send email.", ex);
            }
        }

        public async Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachment, string attachmentFileName)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                {
                    _logger.LogWarning("Invalid email address: {Email}", email);
                    throw new EmailValidationException("Invalid or missing recipient email address.");
                }
                if (string.IsNullOrWhiteSpace(subject))
                {
                    _logger.LogWarning("Missing email subject.");
                    throw new EmailValidationException("Email subject is required.");
                }
                if (string.IsNullOrWhiteSpace(htmlMessage))
                {
                    _logger.LogWarning("Missing email body.");
                    throw new EmailValidationException("Email body is required.");
                }
                if (attachment == null || attachment.Length == 0)
                {
                    _logger.LogWarning("Invalid attachment.");
                    throw new EmailValidationException("Attachment cannot be null or empty.");
                }
                if (string.IsNullOrWhiteSpace(attachmentFileName))
                {
                    _logger.LogWarning("Missing attachment file name.");
                    throw new EmailValidationException("Attachment file name is required.");
                }

                // Sanitize inputs
                subject = _sanitizer.Sanitize(subject);
                htmlMessage = _sanitizer.Sanitize(htmlMessage);

                // Validate SMTP settings
                if (string.IsNullOrEmpty(_smtpSettings.Server) || string.IsNullOrEmpty(_smtpSettings.From))
                {
                    _logger.LogError("SMTP configuration missing (Server or From address).");
                    throw new EmailServiceException("SMTP configuration is incomplete.");
                }

                using var smtpClient = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    EnableSsl = _smtpSettings.EnableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.From),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);
                mailMessage.Attachments.Add(new Attachment(new MemoryStream(attachment), attachmentFileName));

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent to {Email} with attachment {AttachmentFileName}", email, attachmentFileName);

                await PublishEmailSentEvent(new EmailMessage { To = email, Subject = subject, Body = htmlMessage });
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error sending email to {Email} with attachment {AttachmentFileName}", email, attachmentFileName);
                throw new EmailServiceException("Failed to send email with attachment due to SMTP error.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email} with attachment {AttachmentFileName}", email, attachmentFileName);
                throw new EmailServiceException("Failed to send email with attachment.", ex);
            }
        }

        public async Task SendBookingConfirmationEmailAsync(string email, Booking booking, Room room)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                {
                    _logger.LogWarning("Invalid email address: {Email}", email);
                    throw new EmailValidationException("Invalid or missing recipient email address.");
                }
                if (booking == null)
                {
                    _logger.LogWarning("Null booking provided.");
                    throw new EmailValidationException("Booking cannot be null.");
                }
                if (room == null)
                {
                    _logger.LogWarning("Null room provided.");
                    throw new EmailValidationException("Room cannot be null.");
                }

                // Sanitize inputs
                var roomName = _sanitizer.Sanitize(room.Name);
                var subject = _sanitizer.Sanitize("Booking Confirmation");
                var htmlMessage = _sanitizer.Sanitize(
                    $"<h3>Booking Confirmed</h3><p>Your booking for {roomName} from {booking.CheckIn:dd-MM-yyyy} to {booking.CheckOut:dd-MM-yyyy} has been confirmed.</p><p>Total Price: {booking.TotalPrice:C}</p>");

                await SendEmailAsync(email, subject, htmlMessage);
                _logger.LogInformation("Booking confirmation email sent to {Email} for booking {BookingId}", email, booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending booking confirmation email to {Email} for booking {BookingId}", email, booking?.Id);
                throw new EmailServiceException("Failed to send booking confirmation email.", ex);
            }
        }

        public async Task SendBookingCancellationEmailAsync(string email, Booking booking, Room room)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                {
                    _logger.LogWarning("Invalid email address: {Email}", email);
                    throw new EmailValidationException("Invalid or missing recipient email address.");
                }
                if (booking == null)
                {
                    _logger.LogWarning("Null booking provided.");
                    throw new EmailValidationException("Booking cannot be null.");
                }
                if (room == null)
                {
                    _logger.LogWarning("Null room provided.");
                    throw new EmailValidationException("Room cannot be null.");
                }

                // Sanitize inputs
                var roomName = _sanitizer.Sanitize(room.Name);
                var subject = _sanitizer.Sanitize("Booking Cancelled");
                var htmlMessage = _sanitizer.Sanitize(
                    $"<h3>Booking Cancelled</h3><p>Your booking for {roomName} from {booking.CheckIn:dd-MM-yyyy} to {booking.CheckOut:dd-MM-yyyy} has been cancelled.</p>");

                await SendEmailAsync(email, subject, htmlMessage);
                _logger.LogInformation("Booking cancellation email sent to {Email} for booking {BookingId}", email, booking.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending booking cancellation email to {Email} for booking {BookingId}", email, booking?.Id);
                throw new EmailServiceException("Failed to send booking cancellation email.", ex);
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private async Task PublishEmailSentEvent(EmailMessage emailMessage)
        {
            // Placeholder for event publishing (e.g., via MediatR or message queue)
            _logger.LogInformation("Published EmailSentEvent for email to {To}", emailMessage.To);
            await Task.CompletedTask;
        }
    }

    public class EmailServiceException : Exception
    {
        public EmailServiceException(string message) : base(message) { }
        public EmailServiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class EmailValidationException : Exception
    {
        public EmailValidationException(string message) : base(message) { }
    }
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
        Task SendEmailAsync(EmailMessage emailMessage);
        Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachment, string attachmentFileName);
        Task SendBookingConfirmationEmailAsync(string email, Booking booking, Room room);
        Task SendBookingCancellationEmailAsync(string email, Booking booking, Room room);
    }
}