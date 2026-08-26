using FulfillmentInventoryPlatform.Domain.Common;
using FulfillmentInventoryPlatform.Domain.Enums;

namespace FulfillmentInventoryPlatform.Domain.Entities
{
    // Immutable audit trail - never updated or deleted after creation.
    // One row per lifecycle transition (including the initial "created" event,
    // where FromStatus is null).
    public class OrderHistory : BaseEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public OrderStatus? FromStatus { get; set; }
        public OrderStatus ToStatus { get; set; }

        public int ChangedByUserId { get; set; }
        public User ChangedByUser { get; set; } = null!;

        public string? Notes { get; set; }

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
