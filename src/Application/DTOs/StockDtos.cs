using FulfillmentInventoryPlatform.Domain.Enums;

namespace FulfillmentInventoryPlatform.Application.DTOs
{
    // Assigns a product to a warehouse with a starting quantity.
    // Internally recorded as a WarehouseStock starting at 0 plus an InitialStock adjustment.
    public record AssignProductToWarehouseDto(int ProductId, int WarehouseId, int InitialQuantity);

    // Delta can be positive (increase) or negative (decrease)
    public record AdjustStockDto(int ProductId, int WarehouseId, int Delta, StockAdjustmentReason Reason, string? Notes);

    public record WarehouseStockResponseDto(
        int ProductId,
        string ProductName,
        int WarehouseId,
        string WarehouseName,
        int Quantity);

    public record StockAdjustmentResponseDto(
        int Id,
        int ProductId,
        string ProductName,
        int WarehouseId,
        string WarehouseName,
        int Delta,
        int ResultingQuantity,
        string Reason,
        string? Notes,
        string PerformedByUsername,
        DateTime TimestampUtc);
}
