using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Shipping.Api.Data;
using Shipping.Api.Models;
using Shipping.Api.Services;
using System.Text.Json;
using Xunit;

namespace Shipping.Api.tests
{
    public class ShippingMessageReceiverTests
    {
        [Fact]
        public async Task ProcessOrderAsync_ShouldBeIdempotent_WhenOrderIdAlreadyExists()
        {
            // Arrange
            var dbName = "ShippingDb_" + Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ShippingContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;

            // Vi opretter en context til at verificere resultatet til sidst
            using var verifyContext = new ShippingContext(options);
            
            var loggerMock = new Mock<ILogger<ShippingMessageReceiver>>();
            var connectionMock = new Mock<IConnection>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            var serviceProviderMock = new Mock<IServiceProvider>();

            // Setup DI så den returnerer en ny context med de samme options hver gang
            serviceProviderMock
                .Setup(x => x.GetService(typeof(ShippingContext)))
                .Returns(() => new ShippingContext(options));

            scopeMock
                .Setup(x => x.ServiceProvider)
                .Returns(serviceProviderMock.Object);

            scopeFactoryMock
                .Setup(x => x.CreateScope())
                .Returns(scopeMock.Object);

            var receiver = new ShippingMessageReceiver(connectionMock.Object, loggerMock.Object, scopeFactoryMock.Object);

            var orderId = Guid.NewGuid();
            var order = new Order { Id = orderId, ShippingAddress = "Testvej 1, 8000 Aarhus" };
            var message = JsonSerializer.Serialize(order);

            // Act
            // Første kørsel - bør oprette en ShippingOrder
            await receiver.ProcessOrderAsync(order);
            
            // Anden kørsel med samme besked - bør ignorere den (idempotens)
            await receiver.ProcessOrderAsync(order);

            // Assert
            var count = await verifyContext.ShippingOrders.CountAsync(so => so.OrderId == orderId);
            Assert.Equal(1, count);
            
            // Verificer også at logning blev kaldt for skip-scenariet
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already exists")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }
}
