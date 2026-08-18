using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class StockAdjustmentRepository : IStockAdjustmentRepository
    {
        private readonly AppDbContext _db;
        public StockAdjustmentRepository(AppDbContext db) => _db = db;

        public void Add(StockAdjustment adjustment) => _db.StockAdjustments.Add(adjustment);

        public Task<List<StockAdjustment>> GetRecentAsync(int? productId, int? warehouseId, int take, CancellationToken ct = default)
        {
            var query = _db.StockAdjustments
                .Include(a => a.WarehouseStock).ThenInclude(ws => ws.Product)
                .Include(a => a.WarehouseStock).ThenInclude(ws => ws.Warehouse)
                .Include(a => a.PerformedByUser)
                .AsQueryable();

            if (productId.HasValue) query = query.Where(a => a.ProductId == productId.Value);
            if (warehouseId.HasValue) query = query.Where(a => a.WarehouseId == warehouseId.Value);

            return query.OrderByDescending(a => a.TimestampUtc).Take(take).ToListAsync(ct);
        }
    }
}
