using FulfillmentInventoryPlatform.Domain.Common;
using FulfillmentInventoryPlatform.Domain.Enums;

namespace FulfillmentInventoryPlatform.Domain.Entities
{
    // Immutable audit record - never updated or deleted after creation
    public class StockAdjustment : BaseEntity
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public WarehouseStock WarehouseStock { get; set; } = null!;

        // Signed delta: positive = increase, negative = decrease
        public int Delta { get; set; }

        public int ResultingQuantity { get; set; }

        public StockAdjustmentReason Reason { get; set; }

        public string? Notes { get; set; }

        public int PerformedByUserId { get; set; }
        public User PerformedByUser { get; set; } = null!;

        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
