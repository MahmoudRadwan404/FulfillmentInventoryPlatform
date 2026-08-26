using FulfillmentInventoryPlatform.Application.Common;
using FulfillmentInventoryPlatform.Application.DTOs;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    // Order creation, item management, and read/browse operations.
    // Lifecycle transitions (process/complete/cancel) live in IOrderProcessingService.
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateAsync(
            CreateOrderDto dto, int performedByUserId, string? idempotencyKey, CancellationToken ct = default);

        Task<OrderResponseDto> AddItemAsync(
            int orderId, AddOrderItemDto dto, int performedByUserId, CancellationToken ct = default);

        Task<OrderResponseDto> GetByIdAsync(int id, CancellationToken ct = default);

        Task<PagedResult<OrderListItemDto>> GetPagedAsync(OrderQueryDto query, CancellationToken ct = default);

        Task<List<OrderHistoryResponseDto>> GetHistoryAsync(int orderId, int take, CancellationToken ct = default);
    }
}
