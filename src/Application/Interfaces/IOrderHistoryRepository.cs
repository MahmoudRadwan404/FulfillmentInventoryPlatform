using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IOrderHistoryRepository
    {
        void Add(OrderHistory history);

        Task<List<OrderHistory>> GetByOrderAsync(int orderId, int take, CancellationToken ct = default);
    }
}
