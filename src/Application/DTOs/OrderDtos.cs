using FulfillmentInventoryPlatform.Domain.Enums;

namespace FulfillmentInventoryPlatform.Application.DTOs
{
    public record CreateOrderItemDto(int ProductId, int WarehouseId, int Quantity);

    public record CreateOrderDto(int CustomerId, List<CreateOrderItemDto> Items);

    public record AddOrderItemDto(int ProductId, int WarehouseId, int Quantity);

    public record CancelOrderDto(string? Reason);

    public record OrderItemResponseDto(
        int Id,
        int ProductId,
        string ProductName,
        int WarehouseId,
        string WarehouseName,
        decimal UnitPrice,
        int Quantity,
        decimal LineTotal);

    public record OrderResponseDto(
        int Id,
        int CustomerId,
        string CustomerName,
        string Status,
        decimal TotalAmount,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        string? CancellationReason,
        List<OrderItemResponseDto> Items);

    // Lightweight shape for paged order lists - avoids loading every item of every
    // order just to render a browsing table.
    public record OrderListItemDto(
        int Id,
        int CustomerId,
        string CustomerName,
        string Status,
        decimal TotalAmount,
        DateTime CreatedAtUtc);

    public record OrderHistoryResponseDto(
        int Id,
        string? FromStatus,
        string ToStatus,
        string ChangedByUsername,
        string? Notes,
        DateTime TimestampUtc);

    public record OrderQueryDto(
        int Page = 1,
        int PageSize = 20,
        OrderStatus? Status = null,
        int? CustomerId = null,
        string? Search = null,
        string? SortBy = null,
        bool SortDescending = true);
}
