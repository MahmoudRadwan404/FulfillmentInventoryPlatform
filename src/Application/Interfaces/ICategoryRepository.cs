using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface ICategoryRepository
    {
        Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<Category>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
        Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
        Task<bool> HasSubCategoriesOrProductsAsync(int categoryId, CancellationToken ct = default);
        void Add(Category category);
        void Update(Category category);
    }
}
