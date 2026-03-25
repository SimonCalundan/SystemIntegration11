namespace Orders.Api.Models
{
    public class OrdersOutbox
    {
        public Guid Id { get; set; }
        public Order? Payload { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }

    }
}
