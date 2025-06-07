using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Hotel_and_resort.Services
{
    public interface IRabbitMqEventPublisher
    {
        Task PublishAsync(string queueName, object payload);
    }

    public class RabbitMqEventPublisher : IRabbitMqEventPublisher
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqEventPublisher> _logger;

        public RabbitMqEventPublisher(IConfiguration configuration, ILogger<RabbitMqEventPublisher> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task PublishAsync(string queueName, object payload)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
                    UserName = _configuration["RabbitMQ:Username"] ?? "guest",
                    Password = _configuration["RabbitMQ:Password"] ?? "guest",
                    Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672")
                };

                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                // Declare queue asynchronously
                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var message = JsonSerializer.Serialize(payload);
                var body = Encoding.UTF8.GetBytes(message);

                // Publish message asynchronously
                await channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: queueName,
                    body: body);

                _logger.LogInformation("Published event to queue {QueueName}: {Payload}", queueName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event to queue {QueueName}", queueName);
                throw; // Re-throw to allow caller to handle the error appropriately
            }
        }
    }
}