using Microsoft.EntityFrameworkCore;
using Orders.Api.data;
using Orders.Api.Services;
using RabbitMQ.Client;

namespace Shipping.Api.Services
{
    public class OutboxProcessor : BackgroundService
    {
        private readonly ILogger<OutboxProcessor> _logger;
        private readonly IShippingMessageSender messageSender;
        private readonly IServiceProvider _serviceProvider;

        public OutboxProcessor(ILogger<OutboxProcessor> logger, IShippingMessageSender messageSender, IServiceProvider serviceProvider)
        {
            _logger = logger;
            this.messageSender = messageSender;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrdersContext>();
                dbContext.Outbox
                    .Include(o => o.Payload)
                    .Where(o => o.ProcessedAtUtc == null)
                    .ToList()
                    .ForEach(async outbox =>
                {
                    try
                    {
                        if (outbox.Payload != null)
                        {
                            await messageSender.SendMessageAsync(outbox.Payload);
                            outbox.ProcessedAtUtc = DateTime.UtcNow;
                            dbContext.SaveChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing outbox message with id {OutboxId}", outbox.Id);
                    }
                });
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
