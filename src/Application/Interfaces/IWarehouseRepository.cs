using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IWarehouseRepository
    {
        Task<Warehouse?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<Warehouse>> GetAllAsync(bool includeInactive, CancellationToken ct = default);
        Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct = default);
        void Add(Warehouse warehouse);
        void Update(Warehouse warehouse);
    }
}
