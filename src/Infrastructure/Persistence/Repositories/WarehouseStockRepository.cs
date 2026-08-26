using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class WarehouseStockRepository : IWarehouseStockRepository
    {
        private readonly AppDbContext _db;
        public WarehouseStockRepository(AppDbContext db) => _db = db;

        public Task<WarehouseStock?> GetAsync(int productId, int warehouseId, CancellationToken ct = default) =>
            _db.WarehouseStocks
                .Include(ws => ws.Product)
                .Include(ws => ws.Warehouse)
                .FirstOrDefaultAsync(ws => ws.ProductId == productId && ws.WarehouseId == warehouseId, ct);

        public Task<List<WarehouseStock>> GetByProductAsync(int productId, CancellationToken ct = default) =>
            _db.WarehouseStocks
                .Include(ws => ws.Product)
                .Include(ws => ws.Warehouse)
                .Where(ws => ws.ProductId == productId)
                .OrderBy(ws => ws.Warehouse.Name)
                .ToListAsync(ct);

        public Task<List<WarehouseStock>> GetByWarehouseAsync(int warehouseId, CancellationToken ct = default) =>
            _db.WarehouseStocks
                .Include(ws => ws.Product)
                .Include(ws => ws.Warehouse)
                .Where(ws => ws.WarehouseId == warehouseId)
                .OrderBy(ws => ws.Product.Name)
                .ToListAsync(ct);

        public Task<bool> ExistsAsync(int productId, int warehouseId, CancellationToken ct = default) =>
            _db.WarehouseStocks.AnyAsync(ws => ws.ProductId == productId && ws.WarehouseId == warehouseId, ct);

        public void Add(WarehouseStock stock) => _db.WarehouseStocks.Add(stock);
    }
}
