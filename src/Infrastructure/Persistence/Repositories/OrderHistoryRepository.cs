using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class OrderHistoryRepository : IOrderHistoryRepository
    {
        private readonly AppDbContext _db;
        public OrderHistoryRepository(AppDbContext db) => _db = db;

        public void Add(OrderHistory history) => _db.OrderHistories.Add(history);

        public Task<List<OrderHistory>> GetByOrderAsync(int orderId, int take, CancellationToken ct = default) =>
            _db.OrderHistories
                .Include(h => h.ChangedByUser)
                .Where(h => h.OrderId == orderId)
                .OrderByDescending(h => h.TimestampUtc)
                .Take(take)
                .ToListAsync(ct);
    }
}
