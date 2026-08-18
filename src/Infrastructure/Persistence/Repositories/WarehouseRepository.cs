using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly AppDbContext _db;
        public WarehouseRepository(AppDbContext db) => _db = db;

        public Task<Warehouse?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct);

        public async Task<List<Warehouse>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
        {
            var query = _db.Warehouses.AsQueryable();
            if (!includeInactive) query = query.Where(w => w.IsActive);
            return await query.OrderBy(w => w.Name).ToListAsync(ct);
        }

        public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default) =>
            _db.Warehouses.AnyAsync(w => w.Name == name && w.Id != excludeId, ct);

        public void Add(Warehouse warehouse) => _db.Warehouses.Add(warehouse);
        public void Update(Warehouse warehouse) => _db.Warehouses.Update(warehouse);
    }
}
