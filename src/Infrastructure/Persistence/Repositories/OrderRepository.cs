using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using FulfillmentInventoryPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _db;
        public OrderRepository(AppDbContext db) => _db = db;

        public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Items).ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

        public async Task<(List<Order> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            OrderStatus? status,
            int? customerId,
            string? search,
            string? sortBy,
            bool sortDescending,
            CancellationToken ct = default)
        {
            // Deliberately does NOT Include(Items) here - a browsing/list view only
            // needs the order header, so we avoid loading every line of every order
            // just to render a page of the table.
            var query = _db.Orders.Include(o => o.Customer).AsQueryable();

            if (status.HasValue) query = query.Where(o => o.Status == status.Value);
            if (customerId.HasValue) query = query.Where(o => o.CustomerId == customerId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var trimmed = search.Trim();
                if (int.TryParse(trimmed, out var orderId))
                    query = query.Where(o => o.Id == orderId || o.Customer.Name.Contains(trimmed));
                else
                    query = query.Where(o => o.Customer.Name.Contains(trimmed));
            }

            query = (sortBy?.ToLowerInvariant()) switch
            {
                "total" or "totalamount" => sortDescending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
                "status" => sortDescending ? query.OrderByDescending(o => o.Status) : query.OrderBy(o => o.Status),
                _ => sortDescending ? query.OrderByDescending(o => o.CreatedAtUtc) : query.OrderBy(o => o.CreatedAtUtc)
            };

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public void Add(Order order) => _db.Orders.Add(order);
    }
}
