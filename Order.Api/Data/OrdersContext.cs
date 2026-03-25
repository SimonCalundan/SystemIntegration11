using Microsoft.EntityFrameworkCore;
using Orders.Api.Models;

namespace Orders.Api.data
{
    public class OrdersContext : DbContext
    {
        public OrdersContext(DbContextOptions options) :base(options)
        {
            
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrdersOutbox> Outbox { get; set; }
    }
}
