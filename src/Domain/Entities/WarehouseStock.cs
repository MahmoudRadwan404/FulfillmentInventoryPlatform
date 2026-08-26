namespace FulfillmentInventoryPlatform.Domain.Entities
{
    // Composite key: (ProductId, WarehouseId) - configured in Infrastructure
    public class WarehouseStock
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public int Quantity { get; set; }

        // Optimistic concurrency token
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();
    }
}
