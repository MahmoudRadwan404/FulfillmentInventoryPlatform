using FulfillmentInventoryPlatform.Domain.Common;

namespace FulfillmentInventoryPlatform.Domain.Entities
{
    public class OrderItem : BaseEntity
    {
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        // Commercial-value snapshots taken when the item was added to the order.
        // Later changes to Product.Name/Price must never silently rewrite these.
        public string ProductNameSnapshot { get; set; } = string.Empty;
        public decimal UnitPriceSnapshot { get; set; }

        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
