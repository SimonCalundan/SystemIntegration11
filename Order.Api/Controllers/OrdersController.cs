using Microsoft.AspNetCore.Mvc;
using Orders.Api.data;
using Orders.Api.Models;
using Orders.Api.Services;

namespace Orders.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersContext _context;
        private readonly IShippingMessageSender _sender;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            OrdersContext context,
            IShippingMessageSender sender,
            ILogger<OrdersController> logger
            )
        {
            _context = context;
            _sender = sender;
            _logger = logger;
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            order.Id = Guid.NewGuid();
            
            _context.Orders.Add(order);
            _context.Outbox.Add(new OrdersOutbox
            {
                Id = Guid.NewGuid(),
                Payload = order
            });
            await _context.SaveChangesAsync();
            //try
            //{
            //    await _sender.SendMessageAsync(order);
            //}
            //catch (Exception exception)
            //{
            //    _logger.LogError(exception, "Failed to send message to RabbitMQ");

            //}
            return CreatedAtAction("PostOrder", new { id = order.Id }, order);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(Guid id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(Guid id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
