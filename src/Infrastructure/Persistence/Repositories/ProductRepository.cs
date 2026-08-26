using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _db;
        public ProductRepository(AppDbContext db) => _db = db;

        public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, ct);

        public async Task<List<Product>> GetAllAsync(bool includeInactive, int? categoryId, string? search, CancellationToken ct = default)
        {
            var query = _db.Products.Include(p => p.Category).AsQueryable();
            if (!includeInactive) query = query.Where(p => p.IsActive);
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Name.Contains(search));
            return await query.OrderBy(p => p.Name).ToListAsync(ct);
        }

        public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default) =>
            _db.Products.AnyAsync(p => p.Name == name && p.Id != excludeId, ct);

        public void Add(Product product) => _db.Products.Add(product);
        public void Update(Product product) => _db.Products.Update(product);
    }
}
