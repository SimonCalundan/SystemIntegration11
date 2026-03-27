using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shipping.Api.Models;

namespace Shipping.Api.Services
{
    public class ShippingMessageReceiver : IHostedService
    {
        private readonly ILogger<ShippingMessageReceiver> _logger;
        private readonly IConnection _connection;
        private readonly IServiceScopeFactory _scopeFactory;
        private IChannel _channel;

        public ShippingMessageReceiver(IConnection connection, ILogger<ShippingMessageReceiver> logger, IServiceScopeFactory scopeFactory)
        {
            _connection = connection;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _channel.QueueDeclareAsync(queue: "shipping_queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = System.Text.Encoding.UTF8.GetString(body);
                    _logger.LogInformation("Received message: {Message}", message);
                    var order = System.Text.Json.JsonSerializer.Deserialize<Order>(message);
                    if (order == null)
                    {
                        _logger.LogWarning("Received invalid order message: {Message}", message);
                        await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }
                    await ProcessOrderAsync(order);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync(queue: "shipping_queue",
                                 autoAck: false,
                                 consumer: consumer);
        }

        public async Task ProcessOrderAsync(Order order)
        {
            using var scope = _scopeFactory.CreateScope();
            var shippingContext = scope.ServiceProvider.GetRequiredService<Data.ShippingContext>();
            
            var existingShippingOrder = await shippingContext.ShippingOrders.AnyAsync(o => o.OrderId == order.Id);
            if (existingShippingOrder)
            {
                _logger.LogInformation("Shipping order with id {OrderId} already exists", order.Id);
                return;
            }
            var shippingOrder = new ShippingOrder
            {
                ShippingId = Guid.NewGuid(),
                OrderId = order?.Id,
                ShippingAdress = order?.ShippingAddress ?? string.Empty,
                Status = ShippingStatus.Pending
            };
            shippingContext.ShippingOrders.Add(shippingOrder);
            await shippingContext.SaveChangesAsync();
            _logger.LogInformation("Creating shipping order for OrderId: {OrderId}", shippingOrder.OrderId);
            await Task.CompletedTask;

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _channel.Dispose();
            _logger.LogInformation("Stopping ShippingMessageReceiver...");
            return Task.CompletedTask;
        }
    }
}
