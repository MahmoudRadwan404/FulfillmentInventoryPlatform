using FulfillmentInventoryPlatform.Application.DTOs;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    // Owns every order-status transition and the stock side-effects that go with it.
    // Order.Status must NEVER be written to outside this service.
    public interface IOrderProcessingService
    {
        // Pending -> Processing. Checks and deducts stock for every line, atomically.
        // idempotencyKey (optional): if supplied and already used for this endpoint,
        // the original response is replayed instead of deducting stock again.
        Task<OrderResponseDto> ProcessAsync(
            int orderId, int performedByUserId, string? idempotencyKey, CancellationToken ct = default);

        // Processing -> Completed.
        Task<OrderResponseDto> CompleteAsync(int orderId, int performedByUserId, CancellationToken ct = default);

        // Pending -> Cancelled, or Processing -> Cancelled (restores stock exactly once).
        Task<OrderResponseDto> CancelAsync(
            int orderId, CancelOrderDto dto, int performedByUserId, string? idempotencyKey, CancellationToken ct = default);
    }
}
