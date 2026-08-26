using FulfillmentInventoryPlatform.Domain.Common;
using FulfillmentInventoryPlatform.Domain.Enums;

namespace FulfillmentInventoryPlatform.Domain.Entities
{
    public class Order : BaseEntity
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Sum of OrderItem.LineTotal at the time items were added - never recomputed
        // from current product prices, since historical order values must stay stable.
        public decimal TotalAmount { get; set; }

        // Guards stock restore-on-cancel so it can only ever happen once, even if
        // Cancel is somehow invoked twice for the same order (defense in depth
        // alongside the state-machine check in OrderProcessingService).
        public bool StockDeducted { get; set; }

        public string? CancellationReason { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        // Optimistic concurrency token for the order header itself (status/total),
        // independent from WarehouseStock's own RowVersion which protects quantity.
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<OrderHistory> History { get; set; } = new List<OrderHistory>();
    }
}
