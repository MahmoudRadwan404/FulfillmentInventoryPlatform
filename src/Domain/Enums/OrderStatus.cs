namespace FulfillmentInventoryPlatform.Domain.Enums
{
    // Order lifecycle. Valid transitions (enforced in OrderProcessingService):
    //   Pending    -> Processing   (stock is checked and deducted here)
    //   Pending    -> Cancelled    (no stock impact - nothing was deducted yet)
    //   Processing -> Completed    (terminal)
    //   Processing -> Cancelled    (stock restored - see Order.StockDeducted)
    // Completed and Cancelled are both terminal: no further transitions are valid.
    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Cancelled = 3
    }
}
