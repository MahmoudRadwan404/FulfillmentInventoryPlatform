using FulfillmentInventoryPlatform.Domain.Entities;
using FulfillmentInventoryPlatform.Domain.Enums;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IOrderRepository
    {
        // Loads the order with its items (+ product/warehouse) - the shape needed
        // for both displaying an order and processing/cancelling it.
        Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);

        // Paged/filtered/sorted browsing without loading the whole table into memory.
        Task<(List<Order> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            OrderStatus? status,
            int? customerId,
            string? search,
            string? sortBy,
            bool sortDescending,
            CancellationToken ct = default);

        void Add(Order order);
    }
}
