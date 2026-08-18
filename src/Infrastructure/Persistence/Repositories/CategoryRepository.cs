using FulfillmentInventoryPlatform.Application.Interfaces;
using FulfillmentInventoryPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentInventoryPlatform.Infrastructure.Persistence.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _db;
        public CategoryRepository(AppDbContext db) => _db = db;

        public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _db.Categories.Include(c => c.ParentCategory).FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<List<Category>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
        {
            var query = _db.Categories.Include(c => c.ParentCategory).AsQueryable();
            if (!includeInactive) query = query.Where(c => c.IsActive);
            return await query.OrderBy(c => c.Name).ToListAsync(ct);
        }

        public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default) =>
            _db.Categories.AnyAsync(c => c.Name == name && c.Id != excludeId, ct);

        public Task<bool> HasSubCategoriesOrProductsAsync(int categoryId, CancellationToken ct = default) =>
            _db.Categories.AnyAsync(c => c.ParentCategoryId == categoryId, ct);

        public void Add(Category category) => _db.Categories.Add(category);
        public void Update(Category category) => _db.Categories.Update(category);
    }
}
