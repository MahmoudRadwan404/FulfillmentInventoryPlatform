using FulfillmentInventoryPlatform.Domain.Entities;

namespace FulfillmentInventoryPlatform.Application.Interfaces
{
    public interface IWarehouseStockRepository
    {
        Task<WarehouseStock?> GetAsync(int productId, int warehouseId, CancellationToken ct = default);
        Task<List<WarehouseStock>> GetByProductAsync(int productId, CancellationToken ct = default);
        Task<List<WarehouseStock>> GetByWarehouseAsync(int warehouseId, CancellationToken ct = default);
        Task<bool> ExistsAsync(int productId, int warehouseId, CancellationToken ct = default);
        void Add(WarehouseStock stock);
    }
}
