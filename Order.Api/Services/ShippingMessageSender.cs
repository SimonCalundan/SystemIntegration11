using Orders.Api.Models;
using RabbitMQ.Client;

namespace Orders.Api.Services
{
    public class ShippingMessageSender : IShippingMessageSender
    {
        private readonly IConnection _connection;
        private readonly ILogger<ShippingMessageSender> _logger;

        public ShippingMessageSender(IConnection connection, ILogger<ShippingMessageSender> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task SendMessageAsync(Order order)
        {
            try
            {
                using var channel = await _connection.CreateChannelAsync();
                await channel.QueueDeclareAsync(queue: "shipping_queue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                var message = System.Text.Json.JsonSerializer.Serialize(order);
                var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                await channel.BasicPublishAsync(exchange: "",
                                     routingKey: "shipping_queue",
                                     body: bytes);
                _logger.LogInformation("Sent message to RabbitMQ: {Message}", message);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send message to RabbitMQ");
                throw;
            }
        }
    }
}
