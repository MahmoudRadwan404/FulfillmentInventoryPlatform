using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<Product>> GetAllAsync(bool includeInactive, int? categoryId, string? search, CancellationToken ct = default);
        Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
        void Add(Product product);
        void Update(Product product);
    }
}
