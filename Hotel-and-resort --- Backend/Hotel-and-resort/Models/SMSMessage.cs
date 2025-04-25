using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vonage;
using Vonage.Request;


public interface ISmsSenderService
{
        Task SendSmsAsync(string to, string message);
}

    public class SmsSenderService : ISmsSenderService
    {
        private readonly VonageClient _vonageClient;
        private readonly ILogger<SmsSenderService> _logger;

        public SmsSenderService(string apiKey, string apiSecret, ILogger<SmsSenderService> logger)
        {
            var credentials = Credentials.FromApiKeyAndSecret(apiKey, apiSecret);
            _vonageClient = new VonageClient(credentials);
            _logger = logger;
        }

        public async Task SendSmsAsync(string to, string message)
        {
            try
            {
                var response = await _vonageClient.SmsClient.SendAnSmsAsync(new Vonage.Messaging.SendSmsRequest
                {
                    To = to,
                    From = "YourAppName",
                    Text = message
                });

                if (response.Messages[0].Status != "0")
                {
                    _logger.LogError("Failed to send SMS: {ErrorText}", response.Messages[0].ErrorText);
                    throw new Exception("Failed to send SMS: " + response.Messages[0].ErrorText);
                }

                _logger.LogInformation("SMS sent successfully to {Recipient}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {Recipient}", to);
                throw;
            }
        }
    }

